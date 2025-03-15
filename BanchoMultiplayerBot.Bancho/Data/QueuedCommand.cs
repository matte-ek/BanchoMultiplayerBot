namespace BanchoMultiplayerBot.Bancho.Data
{
    /// <summary>
    /// A command that is queued to be executed
    /// </summary>
    public class QueuedCommand
    {
        /// <summary>
        /// The command to send to bancho, does not include arguments and such.
        /// </summary>
        public required string Command { get; set; }

        /// <summary>
        /// The time the command was queued
        /// </summary>
        public DateTime DateTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The list of successful responses from bancho to the command
        /// </summary>
        public required IReadOnlyList<CommandResponse> SuccessfulResponses { get; set; }

        /// <summary>
        /// If the command has been responded to
        /// </summary>
        public bool Responded { get; set; } = false;
    }
}
