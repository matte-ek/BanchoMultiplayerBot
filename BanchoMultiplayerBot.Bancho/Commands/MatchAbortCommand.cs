using BanchoMultiplayerBot.Bancho.Data;
using BanchoMultiplayerBot.Bancho.Interfaces;

namespace BanchoMultiplayerBot.Bancho.Commands;

public class MatchAbortCommand : IBanchoCommand
{
    public static string Command => "!mp abort";

    public static bool AppendSpamFilter => true;

    public static IReadOnlyList<CommandResponse> SuccessfulResponses => [
        new CommandResponse
        {
            Message = "Aborted the match",
            Type = CommandResponseType.Exact
        },
        new CommandResponse
        {
            Message = "The match is not in progress",
            Type = CommandResponseType.Exact
        },
    ];
}