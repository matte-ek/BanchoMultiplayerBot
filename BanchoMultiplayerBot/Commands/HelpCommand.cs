using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Commands
{
    public class HelpCommand : IPlayerCommand
    {
        public string Command => "help";

        public List<string>? Aliases => null;

        public bool AllowGlobal => true;

        public Task ExecuteAsync(IPlayerMessage message)
        {
            message.Reply("");

            return Task.CompletedTask;
        }
    }
}
