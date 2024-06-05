using BanchoMultiplayerBot.Bancho.Data;

namespace BanchoMultiplayerBot.Bancho.Interfaces
{
    public interface IBanchoCommand
    {
        /// <summary>
        /// The bancho command that is used to execute this command, for example "!mp settings".
        /// </summary>
        public static abstract string Command { get; }

        /// <summary>
        /// Whether or not the command message should have characters of the end of the message.
        /// This cannot be used with a command with arguments.
        /// </summary>
        public static abstract bool AppendSpamFilter { get; }

        /// <summary>
        /// List of responses that are considered successful.
        /// </summary>
        public static abstract IReadOnlyList<CommandResponse> SuccessfulResponses { get; }
    }
}
