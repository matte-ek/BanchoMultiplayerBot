using BanchoSharp.Interfaces;

namespace BanchoMultiplayerBot.Behaviour;

public class DebugCommandsBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;

    public void Setup(Lobby lobby)
    {
        _lobby = lobby;
        
        _lobby.OnUserMessage += OnUserMessage;
    }

    private void OnUserMessage(IPrivateIrcMessage msg)
    {
        try
        {
            if (msg.Content.Equals("!uptime"))
            {
                var time = DateTime.Now - _lobby.Bot.RuntimeInfo.StartTime;
                
                _lobby.SendMessage($"{msg.Sender}, current uptime: {time:d' days 'h' hours 'm' minutes 's' seconds'}");
            }
            
            if (msg.Content.Equals("!issuetime"))
            {
                if (!_lobby.Bot.RuntimeInfo.HadNetworkConnectionIssue)
                {
                    _lobby.SendMessage($"{msg.Sender}, no recent connection issues.");
                }
                else
                {
                    var time = DateTime.Now - _lobby.Bot.RuntimeInfo.LastConnectionIssueTime;

                    _lobby.SendMessage($"{msg.Sender}, last connection issue: {time:d' days 'h' hours 'm' minutes 's' seconds'}");   
                }
            }

            if (msg.Content.Equals("!version"))
            {
                _lobby.SendMessage($"{msg.Sender}, current version: {Bot.Version}");
            }
        }
        catch (Exception)
        {
            // ignored
        }
    }
}