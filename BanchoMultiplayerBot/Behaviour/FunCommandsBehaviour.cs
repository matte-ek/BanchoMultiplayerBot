using BanchoMultiplayerBot.Extensions;
using BanchoSharp.Interfaces;

namespace BanchoMultiplayerBot.Behaviour;

public class FunCommandsBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;
    
    public void Setup(Lobby lobby)
    {
        _lobby = lobby;
        
        _lobby.OnUserMessage += OnUserMessage;    
    }

    private void OnUserMessage(IPrivateIrcMessage e)
    {
        try
        {
            var player = _lobby.MultiplayerLobby.Players.FirstOrDefault(x => x.Name.ToIrcNameFormat() == e.Sender);

            if (player is null)
            {
                return;
            }
            
            if (e.Content.Equals("!playtime"))
            {
                var time = DateTime.Now - player.JoinTime;
                
                _lobby.SendMessage($"{e.Sender} has been here for {time:h' hours 'm' minutes 's' seconds'}");
            }
        }
        catch (Exception exception)
        {
            // ignored
        }
    }
}