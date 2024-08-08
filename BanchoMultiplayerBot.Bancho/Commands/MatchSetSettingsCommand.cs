using BanchoMultiplayerBot.Bancho.Data;
using BanchoMultiplayerBot.Bancho.Interfaces;

namespace BanchoMultiplayerBot.Bancho.Commands;

public class MatchSetSettingsCommand : IBanchoCommand
{
    public static string Command => "!mp set";

    public static bool AppendSpamFilter => false;

    public static IReadOnlyList<CommandResponse> SuccessfulResponses => [
        new CommandResponse
        {
            Message = "Changed match settings to ",
            Type = CommandResponseType.StartsWith
        }
    ];
}