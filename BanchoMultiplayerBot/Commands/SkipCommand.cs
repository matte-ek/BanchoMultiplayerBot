using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Commands;

public class SkipCommand : IPlayerCommand
{
    public string Command => "Skip";

    public List<string>? Aliases => ["s"];

    public bool AllowGlobal => false;

    public bool Administrator => false;

    public int MinimumArguments => 0;

    public string? Usage => null;

    // Handled by the behavior
    public Task ExecuteAsync(CommandEventContext context) => Task.CompletedTask;
}