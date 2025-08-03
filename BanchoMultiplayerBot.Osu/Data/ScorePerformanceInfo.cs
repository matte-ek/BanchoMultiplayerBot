using System.Text.Json.Serialization;

namespace BanchoMultiplayerBot.Osu.Data;

public class ScorePerformanceInfo
{
    [JsonPropertyName("pp")]
    public int PerformancePoints { get; init; }
    
    [JsonPropertyName("pp_max")]
    public int MaximumPerformancePoints { get; init; }
}