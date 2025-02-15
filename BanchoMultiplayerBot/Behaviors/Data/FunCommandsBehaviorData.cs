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

    /// <summary>
    /// Whether the lobby is in "temporary teams mode", which can be activated by an admin.
    /// Idea is that the bot won't auto-revert the teams back to normal after a map ends.
    /// </summary>
    public bool InTeamsMode { get; set; }

    /// <summary>
    /// The administrator that activated the teams mode, used to reset the teams mode
    /// after the player that activated it leaves the lobby.
    /// </summary>
    public string TeamsModeActivator { get; set; } = string.Empty;

    public class PlayerTimeRecord
    {
        public string PlayerName { get; init; } = string.Empty;

        public DateTime JoinTime { get; init; }
    }
}