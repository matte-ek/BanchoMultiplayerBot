using BanchoMultiplayerBot.Database.Models;
using BanchoMultiplayerBot.Interfaces;
using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot.Data;

public class CommandEventContext(IPrivateIrcMessage message, string[] arguments, Bot bot, IPlayerCommand playerCommand, User user)
{
    public IPrivateIrcMessage Message { get; } = message;
    
    public string[] Arguments { get; } = arguments;
    
    public IPlayerCommand PlayerCommand { get; } = playerCommand;

    public User User { get; } = user;

    public ILobby? Lobby { get; set; }

    public Bot Bot { get; set; } = bot;
    
    public MultiplayerPlayer? Player { get; set; }

    public void Reply(string message)
    {
        var channel = Message.IsDirect ? Message.Sender : Message.Recipient;
        
        Bot.BanchoConnection.MessageHandler.SendMessage(channel, message);
    }
}