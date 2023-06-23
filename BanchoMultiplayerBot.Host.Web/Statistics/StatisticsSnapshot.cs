namespace BanchoMultiplayerBot.Host.Web.Statistics
{
    public class StatisticsSnapshot
    {

        // Health
        public bool IsConnected { get; set; }
        
        // General
        public int TotalPlayers { get; set; }
        public int MapViolations { get; set; }
        public int HostSkipCount { get; set; }

        // Messages
        public int MessagesReceived { get; set; }
        public int MessagesSent { get; set; }

        // Game stats
        public int GamesPlayed { get; set; }
        public int GamesAborted { get; set; }
        
        // API
        public int TotalApiRequests { get; set; }
        public int TotalApiErrors { get; set; }
        
        public List<StatisticsLobbyData> Lobbies { get; set; } = new();

    }
}
