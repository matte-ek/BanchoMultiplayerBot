using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Behaviors.Data
{
    public sealed class HostQueueBehaviorData : IBehaviorData
    {
        /// <summary>
        /// The player names in the queue, in order. The first element is the current host.
        /// </summary>
        public List<string> Queue { get; set; } = [];

        /// <summary>
        /// The last time the !skip command was used, successfully.
        /// </summary>
        public DateTime HostSkipTime { get; set; }
        
        /// <summary>
        /// Used to temporarily prevent the current host from being skipped after the match ends,
        /// this is used for example when the host disconnected during an ongoing match. 
        /// </summary>
        public bool PreventHostSkip { get; set; }
        
        /// <summary>
        /// The reason we keep track of this ourselves is so we can set and restore this
        /// after the host has been skipped properly, since sometimes the "match finish" event
        /// can happen from bancho which will reset the `MultiplayerLobby.MatchInProgress` but
        /// the behavior won't receive the RoomManagerMatchFinished event.
        /// </summary>
        public bool IsMatchActive { get; set; }
        
        /// <summary>
        /// Recent people who was in the queue and their respective positions, used to restore
        /// positions after eventual crash or networking issues.
        /// </summary>
        public readonly List<PlayerPreviousQueueRecord> PreviousQueueRecords = [];
        
        public class PlayerPreviousQueueRecord
        {
            public string Name { get; init; } = null!;
            
            public int Position { get; init; }
            
            public DateTime Time { get; init; }
        }
    }
}
