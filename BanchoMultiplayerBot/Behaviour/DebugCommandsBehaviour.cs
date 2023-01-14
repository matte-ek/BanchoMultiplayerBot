using BanchoSharp.Interfaces;

namespace BanchoMultiplayerBot.Behaviour;

public class DebugCommandsBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;

    private AutoHostRotateBehaviour? _autoHostRotateBehaviour;
    
    public void Setup(Lobby lobby)
    {
        _lobby = lobby;
        
        _lobby.OnUserMessage += OnUserMessage;

        var autoHostRotateBehaviour = _lobby.Behaviours.Find(x => x.GetType() == typeof(AutoHostRotateBehaviour));
        if (autoHostRotateBehaviour != null)
        {
            _autoHostRotateBehaviour = (AutoHostRotateBehaviour)autoHostRotateBehaviour;
        }
    }

    private void OnUserMessage(IPrivateIrcMessage msg)
    {
        try
        {
            if (msg.Content.Equals("!uptime"))
            {
                var time = DateTime.Now - _lobby.Bot.StartTime;
                
                _lobby.SendMessage($"{msg.Sender}, current uptime: {time:d' days 'h' hours 'm' minutes 's' seconds'}");
            }
            
            if (msg.Content.Equals("!issuetime"))
            {
                if (!_lobby.Bot.HadNetworkConnectionIssue)
                {
                    _lobby.SendMessage($"{msg.Sender}, no recent connection issues.");
                }
                else
                {
                    var time = DateTime.Now - _lobby.Bot.LastConnectionIssueTime;

                    _lobby.SendMessage($"{msg.Sender}, last connection issue: {time:d' days 'h' hours 'm' minutes 's' seconds'}");   
                }
            }
        }
        catch (Exception e)
        {
            // ignored
        }
    }
}