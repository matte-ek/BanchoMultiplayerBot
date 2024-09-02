using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Commands;

public class PerformancePointsCommand : IPlayerCommand
{
    public string Command => "PerformancePoints";

    public List<string>? Aliases => ["pp"];

    public bool AllowGlobal => false;

    public bool Administrator => false;

    public int MinimumArguments => 0;

    public string? Usage => null;

    public Task ExecuteAsync(CommandEventContext context)
    {
        return Task.CompletedTask;
    }
}