namespace BanchoMultiplayerBot.Database.Models;

public class LobbyTimer
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public int LobbyId { get; set; }
    
    public DateTime StartTime { get; set; }
    
    public DateTime EndTime { get; set; }
    
    public bool IsActive { get; set; }
}