using BanchoMultiplayerBot.Bancho.Data;
using BanchoMultiplayerBot.Bancho.Interfaces;

namespace BanchoMultiplayerBot.Bancho.Commands;

public class MatchSetNameCommand : IBanchoCommand
{
    public static string Command => "!mp name";

    public static bool AppendSpamFilter => false;

    public static IReadOnlyList<CommandResponse> SuccessfulResponses => [
        new CommandResponse
        {
            Message = "Room name updated to ",
            Type = CommandResponseType.StartsWith
        }
    ];
}