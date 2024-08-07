using BanchoMultiplayerBot.Bancho.Interfaces;
using BanchoMultiplayerBot.Database.Models;
using BanchoMultiplayerBot.Interfaces;
using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot.Data;

public class CommandEventContext(IPrivateIrcMessage message, string[] arguments, IPlayerCommand playerCommand, User user, IMessageHandler messageHandler)
{
    public IPrivateIrcMessage Message { get; } = message;
    
    public string[] Arguments { get; } = arguments;
    
    public IPlayerCommand PlayerCommand { get; } = playerCommand;

    public User User { get; } = user;

    public ILobby? Lobby { get; set; }
    
    public MultiplayerPlayer? Player { get; set; }

    private readonly IMessageHandler _messageHandler = messageHandler;
    
    public void Reply(string message)
    {
        var channel = Message.IsDirect ? Message.Sender : Message.Recipient;
        
        _messageHandler.SendMessage(channel, message);
    }
}