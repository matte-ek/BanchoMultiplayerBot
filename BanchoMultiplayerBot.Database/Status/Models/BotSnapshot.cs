using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanchoMultiplayerBot.Database.Status.Models
{
    public class BotSnapshot
    {
        public Guid Id { get; set; }

        public DateTime Time { get; set; }

        public int GamesPlayed { get; set; }

        public int MessagesSent { get; set; }
        public int MessageErrorCount { get; set; }

        public int MessagesReceived { get; set; }

        public int ApiLookups { get; set; }
        public int ApiErrorCount { get; set; }

        public int PerformancePointCalculations { get; set; }
        public int PerformancePointCalculationErrors { get; set; }

        public List<LobbySnapshot> LobbySnapshots { get; set; } = new List<LobbySnapshot>();
    }
}
