using BanchoMultiplayerBot.Data;

namespace BanchoMultiplayerBot.Host.WebApi.DataTransferObjects;

public class LobbyModel
{
    public int Id { get; set; }
    
    public string Name { get; set; } = null!;
    
    public LobbyHealth Health { get; set; }
    
    public int PlayerCount { get; set; }
    
    public int PlayerCapacity { get; set; }
}