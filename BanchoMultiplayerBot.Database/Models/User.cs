namespace BanchoMultiplayerBot.Database.Models
{
    public class User
    {
        public int Id { get; set; }

        /// <summary>
        /// osu! user id
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// osu! username
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Total playtime in seconds
        /// </summary>
        public int Playtime { get; set; }

        public int MatchesPlayed { get; set; }

        /// <summary>
        /// Number of times the user has gotten #1 in a match
        /// </summary>
        public int NumberOneResults { get; set; }

        public bool AutoSkipEnabled { get; set; } = false;
        
        public bool Administrator { get; set; } = false;
        
        public virtual ICollection<PlayerBan> Bans { get; } = [];
    }
}
