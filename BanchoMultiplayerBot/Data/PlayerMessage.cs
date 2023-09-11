using BanchoSharp.Messaging.ChatMessages;

namespace BanchoMultiplayerBot.Data;

public class PlayerMessage : PrivateIrcMessage
{
    private Lobby Lobby { get; }
    
    public PlayerMessage(string rawMessage, Lobby lobby) : base(rawMessage)
    {
        Lobby = lobby;
    }
    
    public void Reply(string message, bool carbonCopy = false)
    {
        if (IsDirect)
        {
            Lobby.Bot.SendMessage(Sender, message);

            if (!carbonCopy)
            {
                return;
            }
        }
        
        Lobby.SendMessage(message);
    }
}