namespace BanchoMultiplayerBot.Database.Models;

public class PlayerVote
{
    public int Id { get; set; }
    
    public int LobbyConfigurationId { get; set; }
    
    public int Votes { get; set; }
}