using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Commands;

public class PlayerBanCommand : IPlayerCommand
{
    public string Command => "PlayerBan";

    public List<string>? Aliases => [ "pban" ];

    public bool AllowGlobal => false;

    public bool Administrator => true;

    public int MinimumArguments => 2;

    public string? Usage => "!pban <player_name> <time> <reason>";

    // Handled by the behavior
    public Task ExecuteAsync(CommandEventContext context) => Task.CompletedTask;
}