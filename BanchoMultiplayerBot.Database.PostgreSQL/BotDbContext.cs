using BanchoMultiplayerBot.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace BanchoMultiplayerBot.Database.PostgreSQL;

public class BotDbContext : DbContext
{
    public DbSet<Match> Matches { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<Score> Scores { get; set; }
    public DbSet<PlayerBan> PlayerBans { get; set; }
    public DbSet<MapBan> MapBans { get; set; }
    
    public static string ConnectionString { get; set; } = string.Empty;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(ConnectionString,
            builder => builder.MigrationsAssembly("BanchoMultiplayerBot.Database.PostgreSQL"));
    }
}   