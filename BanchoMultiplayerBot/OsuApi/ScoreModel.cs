using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace BanchoMultiplayerBot.OsuApi;

public class ScoreModel
{
    [JsonPropertyName("beatmap_id")]
    public string? BeatmapId { get; set; }

    [JsonPropertyName("score")]
    public string? Score { get; set; }

    [JsonPropertyName("maxcombo")]
    public string? Maxcombo { get; set; }

    [JsonPropertyName("count50")]
    public string? Count50 { get; set; }

    [JsonPropertyName("count100")]
    public string? Count100 { get; set; }

    [JsonPropertyName("count300")]
    public string? Count300 { get; set; }

    [JsonPropertyName("countmiss")]
    public string? Countmiss { get; set; }

    [JsonPropertyName("countkatu")]
    public string? Countkatu { get; set; }

    [JsonPropertyName("countgeki")]
    public string? Countgeki { get; set; }

    [JsonPropertyName("perfect")]
    public string? Perfect { get; set; }

    [JsonPropertyName("enabled_mods")]
    public string? EnabledMods { get; set; }

    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("rank")]
    public string? Rank { get; set; }

    [JsonPropertyName("score_id")]
    public string? ScoreId { get; set; }

    public string GetRankString()
    {
        return Rank switch
        {
            "X" => "SS",
            "XS" => "SS",
            "SH" => "SS",
            "XH" => "SS",
            _ => Rank ?? "N/A"
        };
    }

    public float CalculateAccuracy()
    {
        var n300 = int.Parse(Count300!);
        var n100 = int.Parse(Count100!);
        var n50 = int.Parse(Count50!);
        var nMiss = int.Parse(Countmiss!);

        return ((n300 * 300 + n100 * 100 + n50 * 50) / (float)((n300 + n100 + n50 + nMiss) * 300)) * 100;
    }
}