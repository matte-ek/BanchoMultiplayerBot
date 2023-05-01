namespace BanchoMultiplayerBot.Host.Web.Statistics
{
    public class StatisticsMinuteSnapshot
    {

        public int MessageThroughput { get; set; }
        public int MessagesSent { get; set; }

        public int GamesPlayed { get; set; }

        public int TotalPlayers { get; set; }

        public int PerformancePointCalcSuccessCount { get; set; }
        public int PerformancePointCalcErrorCount { get; set; }

        public List<StatisticsLobbyMinuteSnapshot> Lobbies { get; set; } = new();

    }
}
