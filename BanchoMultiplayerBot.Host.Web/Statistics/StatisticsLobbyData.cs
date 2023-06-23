namespace BanchoMultiplayerBot.Host.Web.Statistics
{
    public class StatisticsLobbyData
    {

        public string Name { get; set; } = string.Empty;

        public int Players { get; set; }

        public int GamesPlayed { get; set; }
        public int GamesAborted { get; set; }
        
        public int MapViolations { get; set; }
        public int HostSkipCount { get; set; }
        
    }
}
