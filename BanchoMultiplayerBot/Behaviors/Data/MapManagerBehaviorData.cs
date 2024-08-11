using BanchoMultiplayerBot.Data;

namespace BanchoMultiplayerBot.Behaviors.Data;

public class MapManagerBehaviorData
{
    /// <summary>
    /// Currently selected beatmap info
    /// </summary>
    public BeatmapInfo BeatmapInfo { get; set; } = new BeatmapInfo(); 
    
    /// <summary>
    /// The last known good beatmap id that was played
    /// </summary>
    public int BeatmapFallbackId { get; set; } = 2116202;
    
    /// <summary>
    /// The last beatmap id that the host has applied
    /// </summary>
    public int LastPlayerAppliedBeatmapId { get; set; }

    /// <summary>
    /// The beatmap id of the last beatmap that the bot applied
    /// </summary>
    public int LastBotAppliedBeatmapId { get; set; }

    /// <summary>
    /// The amount of times the current host has violated the regulations
    /// </summary>
    public int HostViolationCount { get; set; } = 0;
    
    /// <summary>
    /// The time the match started
    /// </summary>
    public DateTime MatchStartTime { get; set; }
    
    /// <summary>
    /// A list of player names to send the match finish message to
    /// </summary>
    public List<string> MatchFinishMessageSubscribers { get; set; } = [];
}