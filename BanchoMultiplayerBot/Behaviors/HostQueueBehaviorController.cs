using BanchoMultiplayerBot.Bancho.Commands;
using BanchoMultiplayerBot.Interfaces;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Events;

namespace BanchoMultiplayerBot.Behaviors
{
    public class HostQueueBehavior(BotEventContext context) : IBehavior
    {
        private readonly BotEventContext _context = context;
        
        private HostQueueBehaviorData Data => (HostQueueBehaviorData)_context.Data;
        
        [BanchoEvent(BanchoEventType.MatchStarted)]
        public async Task OnMatchStarted()
        {
            await _context.ExecuteCommandAsync<RoomSettingsUpdateCommand>();
        }
        
        [BanchoEvent(BanchoEventType.MatchFinished)]
        public async Task OnMatchFinished()
        {
        }

        [CommandEvent("skip")]
        public async Task OnSkipCommandExecuted()
        {

        }
    }
}