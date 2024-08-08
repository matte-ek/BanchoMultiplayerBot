using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Commands;

public class ForceSkipCommand : IPlayerCommand
{
    public string Command => "ForceSkip";

    public List<string>? Aliases => ["fs"];

    public bool AllowGlobal => false;

    public bool Administrator => true;

    public int MinimumArguments => 0;

    public string? Usage => null;

    // Handled by the behavior
    public Task ExecuteAsync(CommandEventContext context) => Task.CompletedTask;
}