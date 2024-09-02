using BanchoMultiplayerBot.Data;

namespace BanchoMultiplayerBot.Host.WebApi.DataTransferObjects;

public class LobbyExtendedModel : LobbyModel
{
    public BeatmapInfo Beatmap { get; set; } = null!; 
    
    public IEnumerable<string>? Behaviors { get; set; } = null!;
    
    public IEnumerable<PlayerModel>? Players { get; set; } = null;

    public PlayerModel? Host { get; set; } = null;
}