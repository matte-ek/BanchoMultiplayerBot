using BanchoMultiplayerBot.Behaviour;
using BanchoMultiplayerBot.Config;
using BanchoSharp;
using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot;

public class Lobby
{
    
    public Bot Bot { get; }
    public MultiplayerLobby MultiplayerLobby { get; }
    public LobbyConfiguration Configuration { get; }

    public List<IBotBehaviour> Behaviours { get; } = new();

    public event Action? OnLobbyChannelJoined; 

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
        
        Bot.Client.OnChannelJoined += channel =>
        {
            if (channel.ChannelName != _channelName) return;
    
            Console.WriteLine($"Joined: {channel.ChannelName} ({_channelName})");

            OnLobbyChannelJoined?.Invoke();
        };
            
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
    

}