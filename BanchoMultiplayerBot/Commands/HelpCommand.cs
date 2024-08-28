using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Commands
{
    public class HelpCommand : IPlayerCommand
    {
        public string Command => "help";

        public List<string>? Aliases => null;

        public bool AllowGlobal => true;

        public bool Administrator => false;

        public int MinimumArguments { get; } = 0;
        
        public string? Usage { get; } = null; 

        public Task ExecuteAsync(CommandEventContext message)
        {
            message.Reply("todo");
            
            return Task.CompletedTask;
        }
    }
}
