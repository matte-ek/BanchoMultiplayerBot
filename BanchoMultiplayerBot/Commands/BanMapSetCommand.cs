using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Commands;

public class BanMapSetCommand : IPlayerCommand
{
    public string Command => "BanMapset";

    public List<string>? Aliases => [ ];

    public bool AllowGlobal => false;

    public bool Administrator => true;

    public int MinimumArguments => 1;

    public string? Usage => "!banmapset <mapsetid>";

    // Handled by the behavior
    public Task ExecuteAsync(CommandEventContext context) => Task.CompletedTask;
}