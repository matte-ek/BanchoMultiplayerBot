using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Data
{
    public sealed class HostQueueBehaviorData : IBehaviorData
    {
        public List<string> Queue { get; set; } = [];

        public List<PlayerPreviousQueueRecord> PreviousQueueRecords = [];
        
        public class PlayerPreviousQueueRecord
        {
            public string Name { get; set; } = null!;
            
            public int Position { get; set; }
            
            public DateTime Time { get; set; }
        }
    }
}
