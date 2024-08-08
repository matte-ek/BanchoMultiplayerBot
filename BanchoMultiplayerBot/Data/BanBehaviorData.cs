namespace BanchoMultiplayerBot.Data
{
    public sealed class BanBehaviorData
    {
        public Dictionary<string, PlayerJoinedRecord> JoinedRecords { get; set; } = [];

        public class PlayerJoinedRecord
        {
            public string Name { get; set; } = string.Empty;

            public int Frequency { get; set; } = 0;

            public DateTime LastJoinTime { get; set; }
        }
    }
}
