using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanchoMultiplayerBot.Database.Status.Models
{
    /// <summary>
    /// These snapshots would only generate by the hour, and just contains up-time data from the last hour.
    /// The reason for this is it allows up-time data to be stored over a long period of time without using a lot of data.
    /// </summary>
    public class StatusSnapshot
    {
        public Guid Id { get; set; }

        public DateTime Time { get; set; }

        public int SuccessfulStatusCount { get; set; }
    }
}
