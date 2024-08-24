using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Commands;

public class PerformancePointsCommand : IPlayerCommand
{
    public string Command => "PerformancePoints";

    public List<string>? Aliases => ["pp"];

    public bool AllowGlobal => true;

    public bool Administrator => false;

    public int MinimumArguments => 0;

    public string? Usage => null;

    public Task ExecuteAsync(CommandEventContext context)
    {
        context.Reply("This command is not implemented yet.");
        return Task.CompletedTask;
    }
}