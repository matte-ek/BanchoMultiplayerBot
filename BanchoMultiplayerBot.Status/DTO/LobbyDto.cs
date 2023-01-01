namespace BanchoMultiplayerBot.Status.DTO;

public class LobbyDto
{

    public string Name { get; set; } = string.Empty;
    
    public int? BeatmapId { get; set; }
    public int? BeatmapSetId { get; set; }
    
    public int Players { get; set; }
    
    public int GamesPlayed { get; set; }
    
}