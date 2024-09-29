namespace BanchoMultiplayerBot.Host.WebApi.Data;

public class BeatmapCoverData
{
    public int Id { get; set; }
    
    public int LobbyIndex { get; set; }

    public string Data { get; set; } = string.Empty;
}