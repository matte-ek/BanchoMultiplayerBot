namespace BanchoMultiplayerBot.Database.Models;

public class LobbyRoomInstance
{
    public int Id { get; set; }
    
    public int LobbyConfigurationId { get; set; }
    
    public string Channel { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = false;
}