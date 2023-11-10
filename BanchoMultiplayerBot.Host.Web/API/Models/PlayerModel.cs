namespace BanchoMultiplayerBot.Host.Web.API.Models;

public class PlayerModel
{
    public int Id { get; set; }
 
    public long? OsuId { get; set; }
    
    public string Name { get; set; }
    
    public bool IsHost { get; set; }
}