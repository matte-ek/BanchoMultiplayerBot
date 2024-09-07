using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Commands
{
    public class HelpCommand : IPlayerCommand
    {
        public string Command => "help";

        public List<string>? Aliases => [ "commands", "info" ];

        public bool AllowGlobal => true;

        public bool Administrator => false;

        public int MinimumArguments => 0;

        public string? Usage => null;

        public Task ExecuteAsync(CommandEventContext message)
        {
            message.Reply("osu! auto host rotation bot (v2.0.0) [https://github.com/matte-ek/BanchoMultiplayerBot/blob/master/COMMANDS.md Help & Commands]");
            
            return Task.CompletedTask;
        }
    }
}
