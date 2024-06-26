using BanchoMultiplayerBot.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace BanchoMultiplayerBot.Database
{
    public class BotDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Score> Scores { get; set; }
        public DbSet<Map> Maps { get; set; }
        
        public DbSet<PlayerBan> PlayerBans { get; set; }
        public DbSet<MapBan> MapBans { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite($"Data Source=bot.db");
    }
}
