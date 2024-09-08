using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Commands;

public class LastPlayedCommand : IPlayerCommand
{
    public string Command => "LastPlayed";

    public List<string>? Aliases => ["lp"];

    public bool AllowGlobal => false;

    public bool Administrator => false;

    public int MinimumArguments => 0;

    public string? Usage => null;

    // Handled by the behavior
    public Task ExecuteAsync(CommandEventContext context) => Task.CompletedTask;
}