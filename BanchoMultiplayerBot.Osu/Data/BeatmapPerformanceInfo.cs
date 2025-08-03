using System.Text.Json.Serialization;
using BanchoMultiplayerBot.Osu.Interfaces;

namespace BanchoMultiplayerBot.Osu.Data;

public class BeatmapPerformanceInfo
{
    [JsonPropertyName("pp100")]
    public int Performance100 { get; init; }
    
    [JsonPropertyName("pp98")]
    public int Performance98 { get; init; }
    
    [JsonPropertyName("pp95")]
    public int Performance95 { get; init; }
}