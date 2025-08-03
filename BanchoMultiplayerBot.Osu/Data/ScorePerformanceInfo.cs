using System.Text.Json.Serialization;

namespace BanchoMultiplayerBot.Osu.Data;

public class ScorePerformanceInfo
{
    [JsonPropertyName("pp")]
    public float PerformancePoints { get; init; }
    
    [JsonPropertyName("pp_max")]
    public float MaximumPerformancePoints { get; init; }
}