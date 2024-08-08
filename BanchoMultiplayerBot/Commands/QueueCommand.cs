using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Commands;

public class QueueCommand : IPlayerCommand
{
    public string Command => "Queue";

    public List<string>? Aliases => ["q"];

    public bool AllowGlobal => false;

    public bool Administrator => false;

    public int MinimumArguments => 0;

    public string? Usage => null;

    // Handled by the behavior
    public Task ExecuteAsync(CommandEventContext context) => Task.CompletedTask;
}