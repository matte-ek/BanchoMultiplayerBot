namespace BanchoMultiplayerBot.Host.Web.Extra.DTO;

public class MapDTO
{
    public int BeatmapId { get; set; }
    public int? BeatmapSetId { get; set; }

    public string MapName { get; set; } = string.Empty;
    public string? MapArtist { get; set; }
    public string? DifficultyName { get; set; }
    
    public int TimesPlayed { get; set; }
    
    public float? PassPercentage { get; set; }
    public float? LeavePercentage { get; set; }
}