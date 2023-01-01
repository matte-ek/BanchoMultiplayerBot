namespace BanchoMultiplayerBot.Status.DTO;

public class StatusDto
{
    
    public bool Status { get; set; }
    
    public DateTime StartTime { get; set; }
    
    public DateTime LastNetworkIssueTime { get; set; }

    public int Warnings { get; set; }
    
    public float AvgPlayers { get; set; }
    
    public int GamesPlayed { get; set; }
    
    public LobbyDto[]? Lobbies { get; set; }
    
}