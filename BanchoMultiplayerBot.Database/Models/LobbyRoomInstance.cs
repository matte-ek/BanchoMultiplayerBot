namespace BanchoMultiplayerBot.Database.Models;

public class LobbyRoomInstance
{
    public int Id { get; set; }
    
    public int LobbyConfigurationId { get; set; }
    
    /// <summary>
    /// Bancho channel name in the format "#mp_id"
    /// </summary>
    public string Channel { get; set; } = string.Empty;
}