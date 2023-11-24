using BanchoMultiplayerBot.Config;
using BanchoMultiplayerBot.OsuApi;
using BanchoSharp;
using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using Serilog;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Database.PostgreSQL.Repositories;
using BanchoMultiplayerBot.Utilities;
using BanchoMultiplayerBot.Manager;

namespace BanchoMultiplayerBot;

public class Bot
{
    public static string Version => "2.0.0";

    public BanchoClient Client { get; internal set; }
    
    public ConfigurationManager ConfigurationManager { get; init; }
    public StateManager StateManager { get; init; }
    public ConnectionManager ConnectionManager { get; init; }
    public MessageManager MessageManager { get; init; }
    public AnnouncementManager AnnouncementManager { get; init; }
    
    public BotConfiguration Configuration => ConfigurationManager.Configuration;

    public OsuApiWrapper OsuApi { get; internal set; }

    /// <summary>
    /// May or may not be available depending on PerformancePointCalculator.IsAvailable
    /// </summary>
    public PerformancePointCalculator? PerformancePointCalculator { get; private set; }
    
    public List<Lobby> Lobbies { get; } = new();

    public BotRuntimeInfo RuntimeInfo { get; } = new();
    
    public GlobalCommands GlobalCommands { get; }

    public event Action? OnBotReady;
    public event Action? OnLobbiesUpdated;

    public bool WebhookConfigured => Configuration.EnableWebhookNotifications == true && Configuration.WebhookUrl?.Any() == true;
    
    /// <summary>
    /// All lobbies created through CreateLobby, awaiting Bancho to create the room. Gets processed by
    /// OnTournamentLobbyCreated
    /// </summary>
    private readonly List<LobbyConfiguration> _lobbyCreationQueue = new();

    public Bot()
    {
        ConfigurationManager = new ConfigurationManager(this);
        StateManager = new StateManager(this);
        ConnectionManager = new ConnectionManager(this);
        MessageManager = new MessageManager(this);
        AnnouncementManager = new AnnouncementManager();
        
        ConfigurationManager.LoadConfiguration();

        if (PerformancePointCalculator.IsAvailable)
            PerformancePointCalculator = new PerformancePointCalculator();
        else
            Log.Warning($"Could not find 'performance-calculator', pp calculations unavailable.");

        AnnouncementManager.Run(this);

        GlobalCommands = new GlobalCommands(this);

        if (Client == null || Configuration == null || OsuApi == null)
            throw new Exception("Failed to read configuration file.");
    }

    public void SendMessage(string channel, string message)
    {
        MessageManager.SendMessage(channel, message);
    }

    public async Task RunAsync()
    {
        // Register events
        Client.OnAuthenticationFailed += () => throw new Exception($"Failed to authenticate with the username {Configuration.Username}");
        Client.OnAuthenticated += ClientOnAuthenticated;
        Client.OnDisconnected += ClientOnDisconnected;
        Client.OnChannelParted += OnChannelParted;
        Client.BanchoBotEvents.OnTournamentLobbyCreated += OnTournamentLobbyCreated;

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

    public void Shutdown()
    {
        foreach (var lobby in Lobbies)
        {
            lobby.Shutdown();
        }
        
        Lobbies.Clear();
    }

    public async Task DisconnectAsync()
    {
        Shutdown();
        
        ConnectionManager.Stop();

        await Client.DisconnectAsync();

        StateManager.SaveState();
    }

    public async Task CreateLobbyAsync(LobbyConfiguration configuration)
    {
        _lobbyCreationQueue.Add(configuration);

        await Client.MakeTournamentLobbyAsync(configuration.Name);
    }

    public async Task AddLobbyAsync(string channel, LobbyConfiguration configuration)
    {
        var lobby = new Lobby(this, configuration, null!, channel);

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

        var lobby = Lobbies.FirstOrDefault(x => x.Channel == channel.ChannelName);
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

        var lobby = new Lobby(this, config, null!, (MultiplayerLobby)multiplayerLobby);

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
        MessageManager.Start();

        StateManager.LoadState();

        OnBotReady?.Invoke();
        
        ConnectionManager.Start();
        
        RuntimeInfo.Statistics.IsConnected.Set(1);
    }
    
    /// <summary>
    /// Checks if the osu! username specified is added as an bot administrator.
    /// </summary>
    /// TODO: Move this elsewhere.
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
}
