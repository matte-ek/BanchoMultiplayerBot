using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Commands;

public class TeamsModeCommand : IPlayerCommand
{
    public string Command => "teamsmode";

    public List<string>? Aliases => [];

    public bool AllowGlobal => false;

    public bool Administrator => true;

    public int MinimumArguments => 1;

    public string? Usage => "!teamsmode <on/off>";

    // Handled by the behavior
    public Task ExecuteAsync(CommandEventContext context) => Task.CompletedTask;
}