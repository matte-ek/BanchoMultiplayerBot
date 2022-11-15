using System.Text.Json;
using BanchoMultiplayerBot.Config;
using BanchoMultiplayerBot.Config;
using BanchoMultiplayerBot.OsuApi;
using BanchoSharp;
using BanchoSharp.Interfaces;

namespace BanchoMultiplayerBot;

public class Bot
{

    public BanchoClient Client { get; }
    public OsuApiWrapper OsuApi { get; }
    public BotConfiguration Configuration { get; }

    public List<Lobby> Lobbies { get; } = new();

    public event Action? OnBotReady;
    
    public Bot(string configurationFile)
    {
        if (!File.Exists(configurationFile))
        {
            throw new Exception("Failed to find configuration file.");
        }

        var reader = File.OpenRead(configurationFile);
        var config = JsonSerializer.Deserialize<BotConfiguration>(reader);

        Configuration = config ?? throw new Exception("Failed to read configuration file.");
        Client = new BanchoClient(new BanchoClientConfig(new IrcCredentials(Configuration.Username, Configuration.Password), LogLevel.Trace));
        OsuApi = new OsuApiWrapper(config.ApiKey);
    }

    public async Task RunAsync()
    {
        Client.OnAuthenticationFailed += () => throw new Exception($"Failed to authenticate with the username {Configuration.Username}");
        Client.OnAuthenticated += ClientOnAuthenticated;
        Client.OnDisconnected += ClientOnDisconnected;
        Client.OnChannelParted += ClientOnChannelParted;
        
        await Client.ConnectAsync();
    }

    public void CreateLobby()
    {
        // TODO: Integrate this somehow with BanchoSharp, currently BanchoSharp will automatically
        // create a MultiplayerLobby for us.

        throw new NotImplementedException();
    }
    
    public async Task AddLobbyAsync(string channel, LobbyConfiguration configuration)
    {
        var lobby = new Lobby(this, configuration, channel);
        
        Lobbies.Add(lobby);
        
        await lobby.SetupAsync();
    }
    
    private void ClientOnChannelParted(IChatChannel channel)
    {
    }
    
    private void ClientOnDisconnected()
    {
        // TODO: Attempt to reconnect
        // This may be required to be implemented in BanchoSharp
        
        Console.WriteLine("Bot has been disconnected from Bancho!");
    }

    private void ClientOnAuthenticated()
    {
        AutoRecoverExistingLobbies();
        CreateLobbiesFromConfiguration();
        
        OnBotReady?.Invoke();
    }

    private bool AutoRecoverExistingLobbies()
    {
        // TODO: Automatically run the bot on already existing multiplayer lobbies
        // This will be useful if the application were to crash, or is manually restarted.

        return false;
    }

    private void CreateLobbiesFromConfiguration()
    {
        
    }
}