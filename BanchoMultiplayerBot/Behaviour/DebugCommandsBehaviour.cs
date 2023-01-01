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
        if (_autoHostRotateBehaviour == null)
            return;
        
        try
        {
            if (msg.Content.Equals("!uptime"))
            {
                var time = DateTime.Now - _lobby.Bot.StartTime;
                
                _lobby.SendMessage($"{msg.Sender}, current uptime: {time:h' hours 'm' minutes 's' seconds'}");
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

                    _lobby.SendMessage($"{msg.Sender}, last connection issue: {time:h' hours 'm' minutes 's' seconds'}");   
                }
            }
            
            if (msg.Content.StartsWith("!queuepos"))
            {
                var targetName = msg.Sender;
                
                if (msg.Content.StartsWith("!queuepos ")) 
                    targetName = msg.Content.Split("!queuepos ")[1];
                
                if (!_autoHostRotateBehaviour.Queue.Contains(targetName))
                {
                    // Don't really wanna echo back user input, so don't include the player name here.
                    _lobby.SendMessage("Couldn't find player in queue.");
                }
                else
                {
                    var queuePosition = (_autoHostRotateBehaviour.Queue.FindIndex(x => x.Equals(targetName)) + 1).ToString();

                    _lobby.SendMessage($"Queue position for {targetName}: #{queuePosition}");
                }                
            }
        }
        catch (Exception e)
        {
            // ignored
        }
    }
}