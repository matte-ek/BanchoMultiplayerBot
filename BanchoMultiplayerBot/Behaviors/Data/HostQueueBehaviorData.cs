using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Behaviors.Data
{
    public sealed class HostQueueBehaviorData : IBehaviorData
    {
        public List<string> Queue { get; set; } = [];

        public readonly List<PlayerPreviousQueueRecord> PreviousQueueRecords = [];
        
        public class PlayerPreviousQueueRecord
        {
            public string Name { get; init; } = null!;
            
            public int Position { get; init; }
            
            public DateTime Time { get; init; }
        }
    }
}
