using System.ComponentModel.DataAnnotations;

namespace BanchoMultiplayerBot.Database.Models;

public class Map
{
    public int Id { get; set; }
    
    /// <summary>
    /// The osu! beatmap id
    /// </summary>
    public long BeatmapId { get; set; }
    
    /// <summary>
    /// The osu! beatmap set id
    /// </summary>
    public long BeatmapSetId { get; set; }

    [MaxLength(128)] 
    public string BeatmapName { get; set; } = string.Empty;
    
    [MaxLength(128)]
    public string BeatmapArtist { get; set; } = string.Empty;
    
    [MaxLength(128)]
    public string DifficultyName { get; set; } = string.Empty;
    
    public float? StarRating { get; set; }

    public int TimesPlayed { get; set; } = 0;
    
    public float? AveragePassPercentage { get; set; }
    public float? AverageLeavePercentage { get; set; }
    
    public DateTime LastPlayed { get; set; }
}