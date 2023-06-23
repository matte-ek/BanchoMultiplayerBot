namespace BanchoMultiplayerBot.Data;

public class LobbyStatistics
{
    public int GamesPlayed { get; set; }
    public int GamesAborted { get; set; }
    
    public int MapViolationCount { get; set; }
    public int HostSkipCount { get; set; }
}