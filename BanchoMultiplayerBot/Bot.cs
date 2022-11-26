using System.Text.Json;
using BanchoMultiplayerBot.Config;
using BanchoMultiplayerBot.OsuApi;
using BanchoSharp;
using BanchoSharp.Interfaces;
using System.Collections.Concurrent;
using BanchoSharp.Multiplayer;
using BanchoMultiplayerBot.Behaviour;
using Serilog;

namespace BanchoMultiplayerBot;

public class Bot
{

    public BanchoClient Client { get; private set;  }
    public OsuApiWrapper OsuApi { get; }
    public BotConfiguration Configuration { get; }
    public AnnouncementManager AnnouncementManager { get; } = new();

    public List<Lobby> Lobbies { get; } = new();

    public event Action? OnBotReady;
    public event Action? OnLobbiesUpdated;

    private readonly BlockingCollection<QueuedMessage> _messageQueue = new(20);

    private bool _exitRequested = false;

    private readonly List<LobbyConfiguration> _lobbyCreationQueue = new();
    
    public Bot(string configurationFile)
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
        Client = new BanchoClient(new BanchoClientConfig(new IrcCredentials(Configuration.Username, Configuration.Password), LogLevel.Trace));
        OsuApi = new OsuApiWrapper(config.ApiKey);

        AnnouncementManager.Run(this);
    }

    public void SendMessage(string channel, string message)
    {
        _messageQueue.Add(new QueuedMessage()
        {
            Channel = channel,
            Content = message
        });        
    }

    public async Task RunAsync()
    {
        Client.OnAuthenticationFailed += () => throw new Exception($"Failed to authenticate with the username {Configuration.Username}");
        Client.OnAuthenticated += ClientOnAuthenticated;
        Client.OnDisconnected += ClientOnDisconnected;
        Client.OnChannelParted += ClientOnChannelParted;
        Client.BanchoBotEvents.OnTournamentLobbyCreated += OnTournamentLobbyCreated;

        // Events for logging purposes
        Client.OnPrivateMessageReceived += e => { Log.Information($"[{e.Recipient}] {e.Sender}: {e.Content}"); };
        Client.OnPrivateMessageSent += e => { Log.Information($"[{e.Recipient}] {e.Sender}: {e.Content}"); };
        Client.OnChannelJoined += e => { Log.Information($"Joined channel {e.ChannelName}"); };
        Client.OnChannelParted += e => { Log.Information($"Parted channel {e.ChannelName}"); };

        await Client.ConnectAsync();
    }

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

    public async Task DisconnectAsync()
    {
        _exitRequested = true;
        
        await Client.DisconnectAsync();
        
        SaveBotState();
    }

    public async Task CreateLobby(LobbyConfiguration configuration)
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

    private void ClientOnChannelParted(IChatChannel channel)
    {
    }
    
    private void ClientOnDisconnected()
    {
        Console.WriteLine("Bot has been disconnected from Bancho!");
    }

    private void ClientOnAuthenticated()
    {
        Task.Run(RunMessagePump);
        
        if (!AutoRecoverExistingLobbies())
            CreateLobbiesFromConfiguration();
        
        OnBotReady?.Invoke();

        Task.Run(RunConnectionWatchdog);
    }

    private bool AutoRecoverExistingLobbies()
    {
        if (!File.Exists("lobby_states.json"))
            return false;

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

            await CreateLobby(lobbyConfig);
        };

        int lobbyIndex = 0;
        foreach (var lobby in lobbyStates)
        {
            // Attempt to find the correct configuration
            var config = Configuration.LobbyConfigurations?.FirstOrDefault(x => x.Name == lobby.Name);

            if (config == null)
            {
                // Not sure how I want the bot to behave in this case yet.
                return false;
            }

            config.PreviousQueue = lobby.Queue;

            Task.Delay(1 + (lobbyIndex * 4000)).ContinueWith(async task => { await AddLobbyAsync(lobby.Channel, config); });

            lobbyIndex++;
        }
        
        return true;
    }

    private void CreateLobbiesFromConfiguration()
    {
    }

    public void SaveBotState()
    {
        SaveBotConfiguration();

        if (!Lobbies.Any())
            return;
            
        List<LobbyState> lobbyStates = new();

        foreach (var lobby in Lobbies)
        {
            string? queue = null;

            try
            {

                var autoHostRotateBehaviour = lobby.Behaviours.Find(x => x.GetType() == typeof(AutoHostRotateBehaviour));
                if (autoHostRotateBehaviour != null)
                {
                    queue = string.Join(',', ((AutoHostRotateBehaviour)autoHostRotateBehaviour).Queue);
                }
            }
            catch (Exception)
            {
            }

            lobbyStates.Add(new LobbyState()
            {
                Channel = lobby.MultiplayerLobby.ChannelName,
                Name = lobby.Configuration.Name,
                Queue = queue
            });
        }
        
        File.WriteAllText("lobby_states.json", JsonSerializer.Serialize(lobbyStates));
        
        Log.Information($"Saved bot state successfully ({lobbyStates.Count} lobbies)");
    }

    private void SaveBotConfiguration()
    {
        Configuration.LobbyConfigurations = new LobbyConfiguration[Lobbies.Count];

        for (int i = 0; i < Lobbies.Count; i++)
        {
            Configuration.LobbyConfigurations[i] = Lobbies[i].Configuration;
        }

        AnnouncementManager.Save();
        
        File.WriteAllText("config.json", JsonSerializer.Serialize(Configuration));
    }

    private async Task RunConnectionWatchdog()
    {
        int connectionAttempts = 0;
        
        while (!_exitRequested)
        {
            if (!Client.IsConnected)
            {
                Log.Error("DETECTED CONNECTION ERROR!");

                SaveBotState();

                while (connectionAttempts <= 20 && !Client.IsConnected)
                {
                    connectionAttempts++;

                    Log.Information("Attempting to reconnect in 10 seconds");

                    await Task.Delay(10000);
                
                    Client.Dispose();
                    Lobbies.Clear();

                    Client = new BanchoClient(new BanchoClientConfig(new IrcCredentials(Configuration.Username, Configuration.Password), LogLevel.Trace));
                    
                    _ = Task.Run(RunAsync);

                    await Task.Delay(10000);
                }

                break;
            }
            
            await Task.Delay(100);
        }

        if (!_exitRequested)
        {
            Log.Information(Client.IsConnected
                ? "Successfully re-connected to Bancho!"
                : "Failed to restart the bot after 20 attempts.");
        }
    }
    
    private async Task RunMessagePump()
    {
        List<QueuedMessage> sentMessages = new();
        
        try
        {
            while (true)
            {
                var message = _messageQueue.Take();

                bool shouldThrottle;

                do
                {
                    shouldThrottle = sentMessages.Count >= 8;
                    
                    // Remove old messages that are more than 5 seconds old
                    sentMessages.RemoveAll(x => (DateTime.Now - x.Time).Seconds > 5);

                    if (!shouldThrottle) continue;
                    
                    Thread.Sleep(1000);
                } while (shouldThrottle);
                
                message.Time = DateTime.Now;

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
}