namespace BanchoMultiplayerBot.Bancho.Interfaces
{
    public interface ICommandHandler
    {
        public Task<bool> ExecuteAsync<T>(string channel, IReadOnlyList<string>? args = null) where T : IBanchoCommand;
    }
}
