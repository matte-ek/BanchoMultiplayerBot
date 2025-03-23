using BanchoMultiplayerBot.Database.Models;
using BanchoMultiplayerBot.Interfaces;
using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using osu.NET;
using Prometheus;

namespace BanchoMultiplayerBot.Data;

public class CommandEventContext(IPrivateIrcMessage message, string[] arguments, Bot bot, IPlayerCommand playerCommand, User user)
{
    public IPrivateIrcMessage Message { get; } = message;
    
    public string[] Arguments { get; } = arguments;
    
    public IPlayerCommand PlayerCommand { get; } = playerCommand;

    public User User { get; set; } = user;

    public ILobby? Lobby { get; set; }

    public Bot Bot { get; } = bot;
    
    public MultiplayerPlayer? Player { get; set; }

    public void Reply(string message)
    {
        var channel = Message.IsDirect ? Message.Sender : Message.Recipient;
        
        Bot.BanchoConnection.MessageHandler.SendMessage(channel, message);
    }
    
    public async Task<T> UsingApiClient<T>(Func<OsuApiClient, Task<T>> apiCall)
    {
        BotMetrics.ApiRequestsCount.Inc();
        
        using var timer = BotMetrics.ApiRequestsTime.NewTimer();

        try
        {
            return await apiCall(Bot.OsuApiClient);
        }
        catch (OsuApiException)
        {
            BotMetrics.ApiRequestsFailedCount.Inc();
            throw;
        }
    }
}