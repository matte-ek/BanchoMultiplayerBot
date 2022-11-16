using BanchoMultiplayerBot.Behaviour;
using BanchoMultiplayerBot.Config;
using BanchoSharp;
using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot;

public class Lobby
{
    
    public Bot Bot { get; }
    public MultiplayerLobby MultiplayerLobby { get; }
    public LobbyConfiguration Configuration { get; }

    public List<IBotBehaviour> Behaviours { get; } = new();

    public event Action? OnLobbyChannelJoined;
    public event Action<IPrivateIrcMessage>? OnUserMessage;
    public event Action<IPrivateIrcMessage>? OnAdminMessage;

    private readonly string _channelName;
    
    public Lobby(Bot bot, LobbyConfiguration configuration, string channel)
    {
        Bot = bot;
        Configuration = configuration;
        MultiplayerLobby = new MultiplayerLobby(Bot.Client, long.Parse(channel[4..]), channel);

        _channelName = channel;
    }

    public async Task SetupAsync()
    {
        // Add default behaviours
        AddBehaviour(new LobbyManagerBehaviour());
        AddBehaviour(new MapManagerBehaviour());
        
        // Add "custom" behaviours
        if (Configuration.Behaviours != null)
        {
            foreach (var behaviourName in Configuration.Behaviours)
            {
                if (behaviourName == "AutoHostRotate")
                    AddBehaviour(new AutoHostRotateBehaviour());
            }   
        }

        Bot.Client.OnChannelJoined += channel =>
        {
            if (channel.ChannelName != _channelName) return;
    
            Console.WriteLine($"Joined: {channel.ChannelName} ({_channelName})");

            OnLobbyChannelJoined?.Invoke();
        };
        
        Bot.Client.OnPrivateMessageReceived += ClientOnPrivateMessageReceived;
            
        await Bot.Client.JoinChannelAsync(_channelName);
    }

    public async Task SendMessageAsync(string message)
    {
        Console.WriteLine($"Message sent: {message}");   
    }

    private void AddBehaviour(IBotBehaviour behaviour)
    {
        Behaviours.Add(behaviour);
        
        behaviour.Setup(this);
    }
    
    private void ClientOnPrivateMessageReceived(IPrivateIrcMessage message)
    {
        if (message.Recipient != _channelName)
            return;
        if (message.IsBanchoBotMessage)
            return;
        
        OnUserMessage?.Invoke(message);
    }

}