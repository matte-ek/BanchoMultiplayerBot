using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Data
{
    public sealed class HostQueueBehaviorData : IBehaviorData
    {
        public List<string> Queue { get; set; } = [];
    }
}
