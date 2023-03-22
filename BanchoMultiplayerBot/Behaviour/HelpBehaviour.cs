using BanchoSharp.Interfaces;

namespace BanchoMultiplayerBot.Behaviour;

public class HelpBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;
    
    public void Setup(Lobby lobby)
    {
        _lobby = lobby;
        
        _lobby.OnUserMessage += OnUserMessage;
    }

    private void OnUserMessage(IPrivateIrcMessage msg)
    {
        if (msg.Content.Equals("!help") || msg.Content.Equals("!info"))
        {
            _lobby.SendMessage("osu! multiplayer bot [https://github.com/matte-ek/BanchoMultiplayerBot/blob/master/COMMANDS.md Help & Commands]");
        }
    }
}