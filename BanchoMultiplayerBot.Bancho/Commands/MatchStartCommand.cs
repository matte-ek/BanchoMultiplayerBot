using BanchoMultiplayerBot.Bancho.Data;
using BanchoMultiplayerBot.Bancho.Interfaces;

namespace BanchoMultiplayerBot.Bancho.Commands;

public class MatchStartCommand : IBanchoCommand
{
    public static string Command => "!mp start";

    public static bool AppendSpamFilter => false;

    public static IReadOnlyList<CommandResponse> SuccessfulResponses => [ 
        new CommandResponse
        {
            Message = "Started the match",
            Type = CommandResponseType.Exact
        },
        new CommandResponse
        {
            Message = "The match has already been started",
            Type = CommandResponseType.Exact
        },
    ];
}