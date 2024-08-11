using BanchoMultiplayerBot.Data;

namespace BanchoMultiplayerBot.Behaviors.Data;

public class FunCommandsBehaviorData
{
    /// <summary>
    /// The last played beatmap information
    /// </summary>
    public BeatmapInfo? LastPlayedBeatmapInfo { get; set; }

    /// <summary>
    /// The time of when players joined the lobby.
    /// </summary>
    public List<PlayerTimeRecord> PlayerTimeRecords { get; set; } = [];

    /// <summary>
    /// The amount of players that were in the lobby when the map was started.
    /// </summary>
    public int MapStartPlayerCount { get; set; }

    public class PlayerTimeRecord
    {
        public string PlayerName { get; init; } = string.Empty;

        public DateTime JoinTime { get; init; }
    }
}