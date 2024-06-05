using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanchoMultiplayerBot.Bancho.Data
{
    internal class QueuedMessage
    {
        public string Channel { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime Created { get; set; } = DateTime.Now;

        public DateTime Sent { get; set; }

        public TrackedMessageCookie? TrackCookie { get; set; } = null;
    }
}
