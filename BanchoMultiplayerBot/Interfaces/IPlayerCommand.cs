namespace BanchoMultiplayerBot.Interfaces
{
    public interface IPlayerCommand
    {
        /// <summary>
        /// The command itself, for example "help"
        /// </summary>
        public string Command { get; }

        /// <summary>
        /// Alias of the same command, for example "h" for "help", optional.
        /// </summary>
        public List<string>? Aliases { get; }

        /// <summary>
        /// If the command is allowed to be executed globally, for example in a private message directly to the bot.
        /// </summary>
        public bool AllowGlobal { get; }

        public Task ExecuteAsync(IPlayerMessage message);
    }
}
