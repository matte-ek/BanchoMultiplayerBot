using System.Text.Json.Serialization;

namespace BanchoMultiplayerBot.OsuApi;

[Flags]
public enum ModsModel
{
    None           = 0,
    NoFail         = 1,
    Easy           = 2,
    TouchDevice    = 4,
    Hidden         = 8,
    HardRock       = 16,
    SuddenDeath    = 32,
    DoubleTime     = 64,
    Relax          = 128,
    HalfTime       = 256,
    Nightcore      = 512, // Only set along with DoubleTime. i.e: NC only gives 576
    Flashlight     = 1024,
    Autoplay       = 2048,
    SpunOut        = 4096,
    Relax2         = 8192,    // Autopilot
    Perfect        = 16384, // Only set along with SuddenDeath. i.e: PF only gives 16416  
    Key4           = 32768,
    Key5           = 65536,
    Key6           = 131072,
    Key7           = 262144,
    Key8           = 524288,
    FadeIn         = 1048576,
    Random         = 2097152,
    Cinema         = 4194304,
    Target         = 8388608,
    Key9           = 16777216,
    KeyCoop        = 33554432,
    Key1           = 67108864,
    Key3           = 134217728,
    Key2           = 268435456,
    ScoreV2        = 536870912,
    Mirror         = 1073741824,
    KeyMod = Key1 | Key2 | Key3 | Key4 | Key5 | Key6 | Key7 | Key8 | Key9 | KeyCoop,
    FreeModAllowed = NoFail | Easy | Hidden | HardRock | SuddenDeath | Flashlight | FadeIn | Relax | Relax2 | SpunOut | KeyMod,
    ScoreIncreaseMods = Hidden | HardRock | DoubleTime | Flashlight | FadeIn
}

public class BeatmapModel
{
    [JsonPropertyName("beatmapset_id")]
    public string? BeatmapsetId { get; set; }

    [JsonPropertyName("beatmap_id")]
    public string? BeatmapId { get; set; }

    [JsonPropertyName("approved")]
    public string? Approved { get; set; }

    [JsonPropertyName("total_length")]
    public string? TotalLength { get; set; }

    [JsonPropertyName("hit_length")]
    public string? HitLength { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("file_md5")]
    public string? FileMd5 { get; set; }

    [JsonPropertyName("diff_size")]
    public string? DiffSize { get; set; }

    [JsonPropertyName("diff_overall")]
    public string? DiffOverall { get; set; }

    [JsonPropertyName("diff_approach")]
    public string? DiffApproach { get; set; }

    [JsonPropertyName("diff_drain")]
    public string? DiffDrain { get; set; }

    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    [JsonPropertyName("count_normal")]
    public string? CountNormal { get; set; }

    [JsonPropertyName("count_slider")]
    public string? CountSlider { get; set; }

    [JsonPropertyName("count_spinner")]
    public string? CountSpinner { get; set; }

    [JsonPropertyName("submit_date")]
    public string? SubmitDate { get; set; }

    [JsonPropertyName("approved_date")]
    public string? ApprovedDate { get; set; }

    [JsonPropertyName("last_update")]
    public string? LastUpdate { get; set; }

    [JsonPropertyName("artist")]
    public string? Artist { get; set; }

    [JsonPropertyName("artist_unicode")]
    public string? ArtistUnicode { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("title_unicode")]
    public string? TitleUnicode { get; set; }

    [JsonPropertyName("creator")]
    public string? Creator { get; set; }

    [JsonPropertyName("creator_id")]
    public string? CreatorId { get; set; }

    [JsonPropertyName("bpm")]
    public string? Bpm { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("tags")]
    public string? Tags { get; set; }

    [JsonPropertyName("genre_id")]
    public string? GenreId { get; set; }

    [JsonPropertyName("language_id")]
    public string? LanguageId { get; set; }

    [JsonPropertyName("favourite_count")]
    public string? FavouriteCount { get; set; }

    [JsonPropertyName("rating")]
    public string? Rating { get; set; }

    [JsonPropertyName("storyboard")]
    public string? Storyboard { get; set; }

    [JsonPropertyName("video")]
    public string? Video { get; set; }

    [JsonPropertyName("download_unavailable")]
    public string? DownloadUnavailable { get; set; }

    [JsonPropertyName("audio_unavailable")]
    public string? AudioUnavailable { get; set; }

    [JsonPropertyName("playcount")]
    public string? Playcount { get; set; }

    [JsonPropertyName("passcount")]
    public string? Passcount { get; set; }

    [JsonPropertyName("packs")]
    public string? Packs { get; set; }

    [JsonPropertyName("max_combo")]
    public string? MaxCombo { get; set; }

    [JsonPropertyName("diff_aim")]
    public string? DiffAim { get; set; }

    [JsonPropertyName("diff_speed")]
    public string? DiffSpeed { get; set; }

    [JsonPropertyName("difficultyrating")]
    public string? DifficultyRating { get; set; }
}