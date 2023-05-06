using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanchoMultiplayerBot.Database.Status.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class LobbySnapshot
    {
        public Guid Id { get; set; }

        public DateTime Time { get; set; }

        public string Name { get; set; } = string.Empty;

        public int BotLobbyIndex { get; set; }

        public int Players { get; set; }

        public int GamesPlayed { get; set; }

        public int HostViolations { get; set; }
    }
}
