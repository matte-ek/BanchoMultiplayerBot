using BanchoMultiplayerBot.Bancho.Data;
using BanchoMultiplayerBot.Bancho.Interfaces;

namespace BanchoMultiplayerBot.Bancho.Commands;

public class RoomSetPasswordCommand : IBanchoCommand
{
    public static string Command => "!mp password";

    public static bool AppendSpamFilter => false;

    public static IReadOnlyList<CommandResponse> SuccessfulResponses => [
        new CommandResponse
        {
            Message = "Removed the match password",
            Type = CommandResponseType.Exact
        },
        new CommandResponse
        {
            Message = "Changed the match password",
            Type = CommandResponseType.Exact
        }
    ];
}