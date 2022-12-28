using BanchoMultiplayerBot.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace BanchoMultiplayerBot.Database;

public class DatabaseContext : DbContext
{
    
    public DbSet<Player> Players { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite($"Data Source=bot_database.db");
    
}