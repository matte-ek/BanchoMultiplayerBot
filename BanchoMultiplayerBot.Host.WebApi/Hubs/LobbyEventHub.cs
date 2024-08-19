using Microsoft.AspNetCore.SignalR;

namespace BanchoMultiplayerBot.Host.WebApi.Hubs;

public class LobbyEventHub : Hub
{
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }
}