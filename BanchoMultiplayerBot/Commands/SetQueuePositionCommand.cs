using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Commands;

public class SetQueuePositionCommand : IPlayerCommand
{
    public string Command => "SetQueuePosition";

    public List<string>? Aliases => ["sqp", "setqueuepos"];

    public bool AllowGlobal => false;

    public bool Administrator => true;

    public int MinimumArguments => 2;

    public string? Usage => "!setqueuepos <player_name> <position>";

    // Handled by the behavior
    public Task ExecuteAsync(CommandEventContext context) => Task.CompletedTask;
}