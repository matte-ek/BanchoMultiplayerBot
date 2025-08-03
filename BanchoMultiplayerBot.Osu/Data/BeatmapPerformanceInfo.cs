using System.Text.Json.Serialization;
using BanchoMultiplayerBot.Osu.Interfaces;

namespace BanchoMultiplayerBot.Osu.Data;

public class BeatmapPerformanceInfo
{
    [JsonPropertyName("pp100")]
    public float Performance100 { get; init; }
    
    [JsonPropertyName("pp98")]
    public float Performance98 { get; init; }
    
    [JsonPropertyName("pp95")]
    public float Performance95 { get; init; }
}