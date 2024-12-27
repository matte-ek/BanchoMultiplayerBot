using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Commands;

public class BanCommand : IPlayerCommand
{
    public string Command => "Ban";

    public List<string>? Aliases => [ ];

    public bool AllowGlobal => false;

    public bool Administrator => true;

    public int MinimumArguments => 2;

    public string? Usage => "!ban <player_name> <time> <reason>";

    // Handled by the behavior
    public Task ExecuteAsync(CommandEventContext context) => Task.CompletedTask;
}