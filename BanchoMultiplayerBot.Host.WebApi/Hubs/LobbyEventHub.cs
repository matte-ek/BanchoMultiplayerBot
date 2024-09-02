using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace BanchoMultiplayerBot.Host.WebApi.Hubs;

[Authorize]
public class LobbyEventHub : Hub
{
    // Messages are sent out from LobbyTrackerService

    public override Task OnConnectedAsync()
    {
        Log.Information("Client connected to LobbyEventHub");
        
        return base.OnConnectedAsync();
    }
    
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Log.Information("Client disconnected from LobbyEventHub");
        
        return base.OnDisconnectedAsync(exception);
    }
}