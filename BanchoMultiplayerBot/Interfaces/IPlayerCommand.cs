using BanchoMultiplayerBot.Data;
using BanchoSharp.Interfaces;

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
        
        /// <summary>
        /// Whether the command is only allowed to be executed by administrators.
        /// </summary>
        public bool Administrator { get; }
        
        /// <summary>
        /// The minimum amount of arguments required for the command to be executed.
        /// If not reached, the user will be notified of the correct usage.
        /// </summary>
        public int MinimumArguments { get; }
        
        /// <summary>
        /// The command usage, for example "help [command]", optional.
        /// </summary>
        public string? Usage { get; }

        public Task ExecuteAsync(CommandEventContext context);
    }
}
