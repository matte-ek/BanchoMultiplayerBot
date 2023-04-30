namespace BanchoMultiplayerBot.Host.Web.Statistics
{
    public class StatisticsLobbyMinuteSnapshot
    {

        public string Name { get; set; } = string.Empty;

        public int Players { get; set; }

        public int GamesPlayed { get; set; }

        public int HostViolations { get; set; }

    }
}
