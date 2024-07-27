using System.ComponentModel.DataAnnotations;

namespace BanchoMultiplayerBot.Database.Models
{
    public class LobbyBehaviorData
    {
        public int Id { get; set; }

        [Required]
        public int LobbyConfigurationId { get; set; }

        /// <summary>
        /// Dynamic JSON data for behaviour
        /// </summary>
        public string Data { get; set; } = string.Empty;
    }
}
