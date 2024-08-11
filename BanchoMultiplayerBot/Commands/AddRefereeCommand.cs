using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Commands;

public class AddRefereeCommand : IPlayerCommand
{
    public string Command => "AddReferee";

    public List<string>? Aliases => ["addref"];

    public bool AllowGlobal => false;

    public bool Administrator => true;

    public int MinimumArguments => 0;

    public string? Usage => null;

    public Task ExecuteAsync(CommandEventContext context)
    {
        context.Reply($"!mp addref {context.Message.Sender}");

        return Task.CompletedTask;
    }
}