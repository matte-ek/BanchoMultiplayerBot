using BanchoMultiplayerBot.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace BanchoMultiplayerBot.Database
{
    public class BotDbContext : DbContext
    {
        /// <summary>
        /// This is likely a horrible practice, but the bot is not DI,
        /// and will require some rethinking. Since the database needs to be
        /// changed quickly as I am writing this, I will leave it as is.
        /// </summary>
        public static string ConnectionString { get; set; }
        
        public DbSet<User> Users { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Score> Scores { get; set; }

        public DbSet<PlayerBan> PlayerBans { get; set; }
        public DbSet<MapBan> MapBans { get; set; }
        
        public DbSet<NoticeMessage> NoticeMessages { get; set; }

        public DbSet<LobbyConfiguration> LobbyConfigurations { get; set; }
        public DbSet<LobbyRoomInstance> LobbyRoomInstances { get; set; }
        public DbSet<LobbyBehaviorData> LobbyBehaviorData { get; set; }
        public DbSet<LobbyBehaviorConfig> LobbyBehaviorConfig { get; set; }
        public DbSet<LobbyTimer> LobbyTimers { get; set; }
        public DbSet<LobbyVote> LobbyVotes { get; set; }
        
        public DbSet<MapPosition> MapPositions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseNpgsql(ConnectionString);
    }
}
