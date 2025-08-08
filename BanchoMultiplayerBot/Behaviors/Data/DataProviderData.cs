using BanchoMultiplayerBot.Data;

namespace BanchoMultiplayerBot.Behaviors.Data;

public class DataProviderData
{
    /// <summary>
    /// The last played beatmap information
    /// </summary>
    public BeatmapInfo? LastPlayedBeatmapInfo { get; set; }
}