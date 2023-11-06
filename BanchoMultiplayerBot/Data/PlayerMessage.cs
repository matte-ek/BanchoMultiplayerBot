using BanchoMultiplayerBot.Extensions;
using BanchoSharp.Messaging.ChatMessages;
using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot.Data;

public class PlayerMessage : PrivateIrcMessage
{
    private Lobby Lobby { get; }
    
    public MultiplayerPlayer? BanchoPlayer { get; }
    
    public bool IsAdministrator { get; set; }
    
    public PlayerMessage(string rawMessage, Lobby lobby, bool isAdministrator) : base(rawMessage)
    {
        Lobby = lobby;
        IsAdministrator = isAdministrator;
        BanchoPlayer = lobby.MultiplayerLobby.Players.FirstOrDefault(x => x.Name.ToIrcNameFormat() == Sender);
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