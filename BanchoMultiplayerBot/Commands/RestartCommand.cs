using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Commands;

public class RestartCommand : IPlayerCommand
{
    public string Command => "restart";

    public List<string>? Aliases => null;

    public bool AllowGlobal => true;

    public bool Administrator => false;

    public int MinimumArguments => 0;

    public string? Usage => null;

    private static bool _isRestarting;
    
    public Task ExecuteAsync(CommandEventContext message)
    {
        // Race condition can probably appear here, but... no
        if (_isRestarting)
        {
            return Task.CompletedTask;
        }
        
        _isRestarting = true;

        message.Reply("Restarting removed lobbies...");
        
        _ = Task.Run(async () =>
        {
            foreach (var lobby in message.Bot.Lobbies.Where(lobby => lobby.Health == LobbyHealth.EventTimeoutReached))
            {
                await lobby.ConnectAsync();

                int attempts = 0;
                while (!(lobby.Health == LobbyHealth.Ok || lobby.Health == LobbyHealth.Idle))
                {
                    if (attempts++ > 10)
                    {
                        break;
                    }
                        
                    await Task.Delay(1000);
                }
            }

            _isRestarting = false;
        });
        
        return Task.CompletedTask;
    }
}