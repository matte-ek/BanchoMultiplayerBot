using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanchoMultiplayerBot.Data
{
    public class PlayerJoinBan
    {
        public string Name { get; set; } = string.Empty;
        
        public int Frequency { get; set; } = 0;
    
        public DateTime LastJoinTime { get; set; }
    }
}
