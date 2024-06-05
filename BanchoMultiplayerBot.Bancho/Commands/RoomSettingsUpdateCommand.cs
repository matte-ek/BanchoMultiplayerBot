using BanchoMultiplayerBot.Bancho.Data;
using BanchoMultiplayerBot.Bancho.Interfaces;

namespace BanchoMultiplayerBot.Bancho.Commands
{
    public class RoomSettingsUpdateCommand : IBanchoCommand
    {
        public static string Command => "!mp settings";

        public static bool AppendSpamFilter => true;

        public static IReadOnlyList<CommandResponse> SuccessfulResponses => [ 
            new CommandResponse
            {
                Message = "Room name: ",
                Type = CommandResponseType.StartsWith
            }
        ];
    }
}
