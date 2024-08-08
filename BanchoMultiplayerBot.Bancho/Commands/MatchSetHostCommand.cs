using BanchoMultiplayerBot.Bancho.Data;
using BanchoMultiplayerBot.Bancho.Interfaces;

namespace BanchoMultiplayerBot.Bancho.Commands;

/// <summary>
/// This class is unfortunately very scary to use, since Bancho does not give you an error message
/// if the user exists, but is not in the match, which means the command executor will keep on retrying.
/// Not sure how I want to deal with this yet. Marking Obsolete for now, do "!mp host" manually.
/// </summary>
[Obsolete("See class comment.")]
public class MatchSetHostCommand : IBanchoCommand
{
    public static string Command => "!mp host";

    public static bool AppendSpamFilter => false;

    public static IReadOnlyList<CommandResponse> SuccessfulResponses => [
        new CommandResponse
        {
            Message = "Changed match host to ",
            Type = CommandResponseType.StartsWith
        },
        new CommandResponse
        {
            Message = "User not found",
            Type = CommandResponseType.Exact
        },
    ];
}