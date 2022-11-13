using BanchoMultiplayerBot.Models;
using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot;

public class Lobby
{
    
    public Bot Bot { get; }
    public MultiplayerLobby MultiplayerLobby { get; }
    public LobbyConfiguration Configuration { get; }

    private string _channelName;
    
    public Lobby(Bot bot, LobbyConfiguration configuration, string channel)
    {
        Bot = bot;
        Configuration = configuration;
        MultiplayerLobby = new MultiplayerLobby(Bot.Client, long.Parse(channel[4..]), channel);

        _channelName = channel;
    }

    public async Task Setup()
    {
        Bot.Client.OnChannelJoined += channel =>
        {
            if (channel.ChannelName != _channelName) return;

            // Set correct name, size, mode etc
            
        };
            
        await Bot.Client.JoinChannelAsync(_channelName);
    }

    
    

}