using System.Text.Json;
using BanchoMultiplayerBot.Config;
using BanchoMultiplayerBot.Config;
using BanchoMultiplayerBot.OsuApi;
using BanchoSharp;
using BanchoSharp.Interfaces;
using System.Collections.Concurrent;

namespace BanchoMultiplayerBot;

public class Bot
{

    public BanchoClient Client { get; }
    public OsuApiWrapper OsuApi { get; }
    public BotConfiguration Configuration { get; }

    public List<Lobby> Lobbies { get; } = new();

    public event Action? OnBotReady;

    private BlockingCollection<QueuedMessage> messageQueue = new();
    
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

    public void SendMessage(string channel, string message)
    {
        messageQueue.Add(new QueuedMessage()
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

        Task.Run(RunMessagePump);
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

    private async Task RunMessagePump()
    {
        List<QueuedMessage> sentMessages = new();
        
        try
        {
            while (true)
            {
                var message = messageQueue.Take();

                bool shouldThrottle;

                do
                {
                    shouldThrottle = sentMessages.Count >= 3;
                    
                    // Remove old messages that are more than 5 seconds old
                    sentMessages.RemoveAll(x => (DateTime.Now - x.Time).Seconds > 5.1);

                    if (!shouldThrottle) continue;
                   
                    Console.WriteLine($"Throttling messages!");
                
                    Thread.Sleep(1000);
                } while (shouldThrottle);
                
                message.Time = DateTime.Now;

                Console.WriteLine($"Sending message '{message.Content}' from {message.Time} (current queue: {sentMessages.Count})");
                
              //  await Client.SendPrivateMessageAsync(message.Channel, message.Content);
                
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