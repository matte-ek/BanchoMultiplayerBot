namespace BanchoMultiplayerBot.Database.Models;

public class LobbyVote
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public int LobbyId { get; set; }
    
    public bool IsActive { get; set; }
    
    public DateTime StartTime { get; set; }

    public List<string> Votes { get; set; } = [];
}