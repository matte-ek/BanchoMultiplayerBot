using BanchoMultiplayerBot.Behaviour;
using BanchoMultiplayerBot.Models;
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
        Behaviours.Add(new LobbyManagerBehaviour());
        Behaviours.Add(new MapManagerBehaviour());
        
        Bot.Client.OnChannelJoined += channel =>
        {
            if (channel.ChannelName != _channelName) return;
    
            OnLobbyChannelJoined?.Invoke();
        };
            
        await Bot.Client.JoinChannelAsync(_channelName);
    }

    public async Task SendMessageAsync(string message)
    {
        
    }

    private void AddBehaviour(IBotBehaviour behaviour)
    {
        Behaviours.Add(behaviour);
        
        behaviour.Setup(this);
    }
    

}