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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql(@"Host=myserver;Username=mylogin;Password=mypass;Database=mydatabase");
}