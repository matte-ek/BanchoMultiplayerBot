using BanchoMultiplayerBot.Bancho.Interfaces;

namespace BanchoMultiplayerBot.Bancho.Data
{
    public class QueuedCommand
    {

        public required string Command { get; set; }

        public DateTime DateTime { get; set; } = DateTime.Now;

        public required IReadOnlyList<CommandResponse> SuccessfulResponses { get; set; }

        public bool Responded { get; set; } = false;

    }
}
