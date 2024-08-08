using BanchoMultiplayerBot.Bancho.Data;
using BanchoMultiplayerBot.Bancho.Interfaces;

namespace BanchoMultiplayerBot.Bancho.Commands;

public class MatchSetModsCommand : IBanchoCommand
{
    public static string Command => "!mp mods";

    public static bool AppendSpamFilter => false;

    public static IReadOnlyList<CommandResponse> SuccessfulResponses => [
        new CommandResponse
        {
            Message = "Enabled ",
            Type = CommandResponseType.StartsWith
        },
        new CommandResponse
        {
            Message = "Enabled ",
            Type = CommandResponseType.StartsWith
        }
    ];
}