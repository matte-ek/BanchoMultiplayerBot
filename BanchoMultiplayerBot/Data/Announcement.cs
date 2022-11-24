using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanchoMultiplayerBot.Data
{
    public class Announcement
    {

        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// How often to send the message in seconds.
        /// </summary>
        public int Frequency { get; set; }

        /// <summary>
        /// The last time this message was sent in all lobbies
        /// </summary>
        public DateTime LastSent { get; set; }

    }
}
