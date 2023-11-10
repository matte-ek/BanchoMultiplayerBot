using BanchoMultiplayerBot.Config;
using BanchoMultiplayerBot.Data;

namespace BanchoMultiplayerBot.Host.Web.API.Models;

public class LobbyModel
{
    public int Id { get; set; }
    
    public string Name { get; set; }
    
    public LobbyConfiguration Configuration { get; set; }
    
    public BeatmapInfo? BeatmapInfo { get; set; }
    
    public IEnumerable<PlayerModel> Players { get; set; }
}