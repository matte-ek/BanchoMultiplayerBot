using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanchoMultiplayerBot.Data
{
    public class Notification
    {
        public List<string> MessageContext { get; set; } = new List<string>();

        public string TriggerMessage { get; set; } = string.Empty;
    }
}
