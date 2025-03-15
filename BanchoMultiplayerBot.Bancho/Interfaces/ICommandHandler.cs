namespace BanchoMultiplayerBot.Bancho.Interfaces
{
    /// <summary>
    /// Handler for executing bancho multiplayer commands, in a more structured way.
    /// </summary>
    public interface ICommandHandler
    {
        /// <summary>
        /// Execute a Bancho command and return the result. Bancho commands are provided in the form of a class that implements <see cref="IBanchoCommand"/>.
        /// Which are available at <see cref="BanchoMultiplayerBot.Bancho.Commands"/>.
        /// </summary>
        /// <param name="channel">The bancho multiplayer room to send the command in</param>
        /// <param name="args">Optional arguments</param>
        /// <typeparam name="T">Bancho command to send</typeparam>
        /// <returns>If the command was executed successfully</returns>
        public Task<bool> ExecuteAsync<T>(string channel, IReadOnlyList<string>? args = null) where T : IBanchoCommand;
    }
}
