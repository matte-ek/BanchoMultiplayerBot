using BanchoMultiplayerBot.Bancho.Data;
using BanchoMultiplayerBot.Bancho.Interfaces;

namespace BanchoMultiplayerBot.Bancho.Commands;

public class MatchSetBeatmapCommand : IBanchoCommand
{
    public static string Command => "!mp map";

    public static bool AppendSpamFilter => false;

    public static IReadOnlyList<CommandResponse> SuccessfulResponses => [
        new CommandResponse
        {
            Message = "Changed beatmap to ",
            Type = CommandResponseType.StartsWith
        },
        new CommandResponse
        {
            Message = "Invalid map ID provided",
            Type = CommandResponseType.Exact
        }
    ];
}