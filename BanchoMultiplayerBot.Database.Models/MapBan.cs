namespace BanchoMultiplayerBot.Database.Models;

public class MapBan
{
    public int Id { get; set; }
    
    public int? BeatmapSetId { get; set; }
    
    public int? BeatmapId { get; set; }
}