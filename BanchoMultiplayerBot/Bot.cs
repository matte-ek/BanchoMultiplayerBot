using BanchoMultiplayerBot.Config;
using BanchoMultiplayerBot.OsuApi;
using BanchoSharp;
using BanchoSharp.Interfaces;
using System.Collections.Concurrent;
using System.Net.Sockets;
using BanchoSharp.Multiplayer;
using BanchoMultiplayerBot.Behaviour;
using Serilog;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Database.Repositories;
using BanchoMultiplayerBot.Utilities;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace BanchoMultiplayerBot;

public class Bot
{
    public static string Version => "1.5.4";

    public BanchoClient Client { get; private set; }

    public OsuApiWrapper OsuApi { get; private set; }

    /// <summary>
    /// May or may not be available depending on PerformancePointCalculator.IsAvailable
    /// </summary>
    public PerformancePointCalculator? PerformancePointCalculator { get; private set; }

    public AnnouncementManager AnnouncementManager { get; } = new();

    public BotConfiguration Configuration { get; private set; }

    public List<Lobby> Lobbies { get; } = new();

    public BotRuntimeInfo RuntimeInfo { get; } = new();
    
    public GlobalCommands GlobalCommands { get; }

    public event Action? OnBotReady;
    public event Action? OnLobbiesUpdated;

    public bool WebhookConfigured => Configuration.EnableWebhookNotifications == true && Configuration.WebhookUrl?.Any() == true;

    /// <summary>
    /// Message queue which is part of the message rate limiting system.
    /// </summary>
    private readonly BlockingCollection<QueuedMessage> _messageQueue = new(20);

    /// <summary>
    /// List of messages the message pump will internally ignore, we do this because there is no way
    /// of safely removing messages in the message queue.
    /// </summary>
    private readonly List<Guid> _ignoredMessages = new();
    
    /// <summary>
    /// All lobbies created through CreateLobby, awaiting Bancho to create the room. Gets processed by
    /// OnTournamentLobbyCreated
    /// </summary>
    private readonly List<LobbyConfiguration> _lobbyCreationQueue = new();

    private DateTime _lastMessageTime;

    private bool _exitRequested;

    private string _lastSentMessage = string.Empty;
    
    public Bot(string configurationFile)
    {
        LoadConfiguration(configurationFile);

        if (PerformancePointCalculator.IsAvailable)
            PerformancePointCalculator = new PerformancePointCalculator();
        else
            Log.Warning($"Could not find 'performance-calculator', pp calculations unavailable.");

        AnnouncementManager.Run(this);

        GlobalCommands = new GlobalCommands(this);

        if (Client == null || Configuration == null || OsuApi == null)
            throw new Exception("Failed to read configuration file.");
    }

    /// <summary>
    /// Saves the current lobby configurations, but not the currently active lobbies.
    /// </summary>
    public void SaveConfiguration()
    {
        Configuration.LobbyConfigurations = new LobbyConfiguration[Lobbies.Count];

        for (int i = 0; i < Lobbies.Count; i++)
        {
            Configuration.LobbyConfigurations[i] = Lobbies[i].Configuration;
        }

        AnnouncementManager.Save();

        File.WriteAllText("config.json", JsonSerializer.Serialize(Configuration));
    }
    
    /// <summary>
    /// Saves all the current lobbies channels and queue and their configuration. When everything is saved, the bot may pick up
    /// where it left via AutoRecoverExistingLobbies()
    /// </summary>
    public void SaveBotState()
    {
        SaveConfiguration();

        if (!Lobbies.Any())
            return;

        List<LobbyState> lobbyStates = new();

        foreach (var lobby in Lobbies)
        {
            if (lobby.IsParted)
                continue;
            
            string? queue = null;
            List<PlaytimeRecord> playtimeRecords = new();

            try
            {
                // Special case for AutoHostRotateBehaviour, this should be done differently if any more of these cases come up.
                // This will save the queue, so that also gets recovered successfully.
                if (lobby.Behaviours.Find(x => x.GetType() == typeof(AutoHostRotateBehaviour)) is AutoHostRotateBehaviour autoHostRotateBehaviour)
                {
                    queue = string.Join(',', autoHostRotateBehaviour.Queue);
                }

                playtimeRecords.AddRange(lobby.MultiplayerLobby.Players.Select(player => new PlaytimeRecord() { Name = player.Name, JoinTime = player.JoinTime.ToBinary()}));
            }
            catch (Exception)
            {
                // ignored
            }

            lobbyStates.Add(new LobbyState()
            {
                Channel = lobby.MultiplayerLobby.ChannelName,
                Name = lobby.Configuration.Name,
                Queue = queue,
                PlayerPlaytime = playtimeRecords.ToArray()
            });
        }

        File.WriteAllText("lobby_states.json", JsonSerializer.Serialize(lobbyStates));

        Log.Information($"Saved bot state successfully ({lobbyStates.Count} lobbies)");
    }

    public void LoadConfiguration(string configurationFile)
    {
        if (!File.Exists(configurationFile))
        {
            throw new Exception($"Failed to find configuration file");
        }

        var reader = File.OpenRead(configurationFile);
        var config = JsonSerializer.Deserialize<BotConfiguration>(reader);

        reader.Close();
        reader.Dispose();

        Configuration = config ?? throw new Exception("Failed to read configuration file.");
        Client = new BanchoClient(new BanchoClientConfig(new IrcCredentials(Configuration.Username, Configuration.Password), LogLevel.Trace, false));
        OsuApi = new OsuApiWrapper(this, Configuration.ApiKey);
    }

    /// <summary>
    /// Sends a message to the channel (may also be username), will also handle rate limiting and anti-spam automatically.
    /// </summary>
    public void SendMessage(string channel, string message)
    {
        _messageQueue.Add(new QueuedMessage()
        {
            Channel = channel,
            Content = message
        });

        _lastSentMessage = message;
    }

    public async Task RunAsync()
    {
        // Register events
        Client.OnAuthenticationFailed += () => throw new Exception($"Failed to authenticate with the username {Configuration.Username}");
        Client.OnAuthenticated += ClientOnAuthenticated;
        Client.OnDisconnected += ClientOnDisconnected;
        Client.OnChannelParted += OnChannelParted;
        Client.BanchoBotEvents.OnTournamentLobbyCreated += OnTournamentLobbyCreated;
        Client.OnPrivateMessageReceived += OnPrivateMessageReceived;

        // Events for logging purposes
        Client.OnPrivateMessageReceived += e =>
        {
            RuntimeInfo.Statistics.MessagesReceived.Inc();
            Log.Information($"[{e.Recipient}] {e.Sender}: {e.Content}");
        };
        
        Client.OnPrivateMessageSent += e =>
        {
            RuntimeInfo.Statistics.MessagesSent.Inc();
            Log.Information($"[{e.Recipient}] {e.Sender}: {e.Content}");
        };
        
        Client.OnChannelJoined += e => { Log.Information($"Joined channel {e.ChannelName}"); };
        Client.OnChannelParted += e => { Log.Information($"Parted channel {e.ChannelName}"); };

        RuntimeInfo.StartTime = DateTime.Now;

        GlobalCommands.Setup();
        
        Lobbies.Clear();

        await Client.ConnectAsync();
    }

    private void OnPrivateMessageReceived(IPrivateIrcMessage msg)
    {
        _lastMessageTime = DateTime.Now;
        
        try
        {
            if (msg.Recipient == "#osu" && 
                msg.IsBanchoBotMessage &&
                msg.Content.StartsWith("Bancho will be restarting for maintenance in 1 minute."))
            {
                if (WebhookConfigured && Configuration.WebhookNotifyConnectionErrors == true)
                {
                    _ = WebhookUtils.SendWebhookMessage(Configuration.WebhookUrl!, "Bancho Restart", $"Bancho is queued to restart within a minute.");
                }
            
                AnnouncementManager.SendAnnouncementMessage("Bancho is about to restart, the lobby should be automatically re-created in few minutes after Bancho is restarted.");
                AnnouncementManager.SendAnnouncementMessage("Try searching for the lobby if you cannot find it in the list, thanks for playing!");
            }
        }
        catch (Exception e)
        {
            Log.Error($"{e}");
        }
    }

    public async Task DisconnectAsync()
    {
        _exitRequested = true;

        await Client.DisconnectAsync();

        SaveBotState();
    }

    public async Task CreateLobbyAsync(LobbyConfiguration configuration)
    {
        _lobbyCreationQueue.Add(configuration);

        await Client.MakeTournamentLobbyAsync(configuration.Name);
    }

    public async Task AddLobbyAsync(string channel, LobbyConfiguration configuration)
    {
        var lobby = new Lobby(this, configuration, channel);

        Lobbies.Add(lobby);

        await lobby.SetupAsync();

        OnLobbiesUpdated?.Invoke();
    }

    public void RemoveLobby(Lobby lobby)
    {
        Lobbies.Remove(lobby);
        OnLobbiesUpdated?.Invoke();
    }

    private void OnChannelParted(IChatChannel channel)
    {
        if (WebhookConfigured && Configuration.WebhookNotifyLobbyTerminations == true)
        {
            _ = WebhookUtils.SendWebhookMessage(Configuration.WebhookUrl!, "Channel Closed", $"Channel {channel.ChannelName} was closed.");
        }

        var lobby = Lobbies.Where(x => x.Channel == channel.ChannelName)?.FirstOrDefault();
        if (lobby != null)
        {
            lobby.IsParted = true;
        }

        Log.Warning($"Channel {channel.ChannelName} was parted.");
    }

    /// <summary>
    /// Handle newly created lobbies that were created via the bot, other lobbies are ignored.
    /// </summary>
    private async void OnTournamentLobbyCreated(IMultiplayerLobby multiplayerLobby)
    {
        var config = _lobbyCreationQueue.Find(x => x.Name == multiplayerLobby.Name);

        // Lobby probably wasn't made by the bot.
        if (config == null)
        {
            Log.Warning($"Tournament lobby created without configuration");
            return;
        }

        _lobbyCreationQueue.Remove(config);

        var lobby = new Lobby(this, config, (MultiplayerLobby)multiplayerLobby);

        Lobbies.Add(lobby);

        await lobby.SetupAsync(true);

        OnLobbiesUpdated?.Invoke();
    }

    private void ClientOnDisconnected()
    {
        Log.Error("Bot has been disconnected from Bancho!");
    }

    private void ClientOnAuthenticated()
    {
        _lastMessageTime = DateTime.Now;
        _exitRequested = false;

        Task.Run(RunMessagePump);

        RecoverExistingLobbies();

        OnBotReady?.Invoke();

        Task.Run(RunConnectionWatchdog);
        
        RuntimeInfo.Statistics.IsConnected.Set(1);
    }

    /// <summary>
    /// Attempts to rejoin lobbies that were previously created. Allows the bot to fully recover from
    /// restarts, network issues, bancho restarts and whatnot. If the previous lobbies were not found, 
    /// they will be created.
    /// </summary>
    private bool RecoverExistingLobbies()
    {
        // All previous lobby information is stored in lobby_states.json
        if (!File.Exists("lobby_states.json"))
            return false;

        // Load and parse the JSON file
        var reader = File.OpenRead("lobby_states.json");
        var lobbyStates = JsonSerializer.Deserialize<List<LobbyState>>(reader);

        reader.Close();
        reader.Dispose();

        if (lobbyStates == null)
            return false;

        Log.Information("Recovering existing lobbies...");

        Client.OnChannelJoinFailure += async name =>
        {
            // Attempt to create a new lobby instead.
            var lobbyName = lobbyStates.FirstOrDefault(x => x.Channel == name);
            var lobbyConfig = Configuration.LobbyConfigurations?.FirstOrDefault(x => x.Name == lobbyName?.Name);

            if (lobbyConfig == null)
            {
                return;
            }

            Log.Warning($"Failed to find lobby by name {lobbyName?.Name}, creating new one instead.");

            var failedLobby = Lobbies.FirstOrDefault(x => x.Configuration.Name ==  lobbyName?.Name);
            if (failedLobby != null)
            {
                Lobbies.Remove(failedLobby);
            }

            await CreateLobbyAsync(lobbyConfig);
        };

        int lobbyIndex = 0;
        foreach (var lobby in lobbyStates.OrderBy(x => x.Name).ToList())
        {
            // Attempt to find the correct configuration within our lobby configurations.
            var config = Configuration.LobbyConfigurations?.FirstOrDefault(x => x.Name == lobby.Name);

            if (config == null)
            {
                // Not sure how I want the bot to behave in this case yet, return is intentional. 
                Log.Error($"Failed to find configuration for lobby during recovery.");

                return false;
            }
            
            config.PreviousQueue = lobby.Queue;
            config.PlayerPlaytime = lobby.PlayerPlaytime;

            // Wait 4 seconds between each lobby, caused issues otherwise.
            Task.Delay(lobbyIndex * 1000).ContinueWith(async task => { await AddLobbyAsync(lobby.Channel, config); });

            lobbyIndex++;
        }

        return true;
    }

    /// <summary>
    /// Task to continuously monitor the TCP connection to Bancho, and in case of an network issue
    /// automatically attempt to reconnect to Bancho and restore the bot.
    /// </summary>
    private async Task RunConnectionWatchdog()
    {
        int connectionAttempts = 0;

        while (!_exitRequested)
        {
            if (!IsTcpConnectionAlive(Client.TcpClient))
            {
                Log.Error("[!] DETECTED CONNECTION ERROR!");

                if (WebhookConfigured && Configuration.WebhookNotifyConnectionErrors == true)
                {
                    _ = WebhookUtils.SendWebhookMessage(Configuration.WebhookUrl!, "Connection Error", $"Detected connection error to osu!bancho");
                }

                RuntimeInfo.HadNetworkConnectionIssue = true;
                RuntimeInfo.LastConnectionIssueTime = DateTime.Now;
                
                RuntimeInfo.Statistics.IsConnected.Set(0);

                SaveBotState();

                while (connectionAttempts <= 20 && !IsTcpConnectionAlive(Client.TcpClient))
                {
                    connectionAttempts++;

                    Log.Information("Attempting to reconnect in 30 seconds");

                    await Task.Delay(30000);

                    Client.Dispose();
                    Lobbies.Clear();

                    Client = new BanchoClient(new BanchoClientConfig(new IrcCredentials(Configuration.Username, Configuration.Password), LogLevel.Trace));

                    _ = Task.Run(RunAsync);

                    await Task.Delay(10000);
                }

                break;
            }

            try
            {
                // This is an additional fail-safe for the connection state, by checking the last time we received a message,
                // so if we haven't received a message for 5 minutes, then write a message to test the connection.
                // I feel like 5 minutes is a pretty safe bet for now.
                if ((DateTime.Now - _lastMessageTime).TotalSeconds > 300)
                {
                    _lastMessageTime = DateTime.Now;

                    SendMessage("BanchoBot", $"connection check: {DateTime.Now}");

                    Log.Warning("No message for 5 minutes, testing connection by sending a message to BanchoBot.");
                }
            }
            catch (Exception)
            {
                // ignored
            }

            await Task.Delay(1000);
        }

        if (!_exitRequested)
        {
            Log.Information(Client.IsConnected
                ? "Successfully re-connected to Bancho!"
                : "Failed to restart the bot after 20 attempts.");
        }
    }

    /// <summary>
    /// Task to send all messages within the queue, and handle rate limits for the messages.
    /// </summary>
    private async Task RunMessagePump()
    {
        int messageBurstCount = Configuration.IsBotAccount == true ? 300 : 8;
        int messageAge = Configuration.IsBotAccount == true ? 60 : 6;
        const int maxMessageLength = 400;

        List<QueuedMessage> sentMessages = new();

        try
        {
            while (true)
            {
                var message = _messageQueue.Take();

                if (_ignoredMessages.Contains(message.Id))
                {
                    _ignoredMessages.Remove(message.Id);
                    continue;
                }
                
                RuntimeInfo.Statistics.MessageSendQueue.Set(_messageQueue.Count);
                
                bool shouldThrottle;

                do
                {
                    shouldThrottle = sentMessages.Count >= messageBurstCount - 3;

                    // Remove old messages that are more than 5 seconds old
                    sentMessages.RemoveAll(x => (DateTime.Now - x.Time).Seconds > messageAge);

                    if (!shouldThrottle) continue;

                    Thread.Sleep(1000);
                } while (shouldThrottle);

                message.Time = DateTime.Now;

                // Ideally the messages should maybe just get trimmed here and sent anyway, but this isn't really
                // meant as an convenience, it's more of a fail-safe to never exceed the message limit. 
                if (message.Content.Length >= maxMessageLength)
                {
                    Log.Warning($"Ignoring message '{message.Content}', message is too long.");
                    continue;
                }

                Log.Verbose($"Sending message '{message.Content}' from {message.Time} (current queue: {sentMessages.Count})");
                
                try
                {
                    await Client.SendPrivateMessageAsync(message.Channel, message.Content);
                }
                catch (Exception e)
                {
                    Log.Error($"Error while sending message: {e.Message}");
                }

                sentMessages.Add(message);
            }
        }
        catch (InvalidOperationException)
        {
            // An InvalidOperationException means that Take() was called on a completed collection,
            // so we'll just exit out off this thread normally.
        }
    }
    
    /// <summary>
    /// Checks if the osu! username specified is added as an bot administrator.
    /// </summary>
    internal static async Task<bool> IsAdministrator(string username)
    {
        if (!username.Any())
        {
            return false;
        }

        try
        {
            using var userRepository = new UserRepository();

            var user = await userRepository.FindUser(username);

            return user != null && user.Administrator;
        }
        catch (Exception e)
        {
            Log.Error( $"Error while querying administrator status for user: {e}");
            return false;
        }
    }

    // See https://stackoverflow.com/a/6993334
    private static bool IsTcpConnectionAlive(TcpClient? client)
    {
        try
        {
            if (client != null && client.Client.Connected)
            {
                // Detect if client disconnected
                if (!client.Client.Poll(0, SelectMode.SelectRead))
                    return true;

                byte[] buff = new byte[1];

                return client.Client.Receive(buff, SocketFlags.Peek) != 0;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}
