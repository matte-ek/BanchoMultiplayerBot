using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Commands;

public class SetHostCommand : IPlayerCommand
{
    public string Command => "SetHost";

    public List<string>? Aliases => ["sh"];

    public bool AllowGlobal => false;

    public bool Administrator => true;

    public int MinimumArguments => 1;

    public string? Usage => "!sethost <player_name>";

    // Handled by the behavior
    public Task ExecuteAsync(CommandEventContext context) => Task.CompletedTask;
}