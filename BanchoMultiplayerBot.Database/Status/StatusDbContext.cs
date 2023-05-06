using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BanchoMultiplayerBot.Database.Status.Models;
using Microsoft.EntityFrameworkCore;

namespace BanchoMultiplayerBot.Database.Status
{
    public class StatusDbContext : DbContext
    {
        public DbSet<BotSnapshot> BotSnapshots { get; set; }
        public DbSet<StatusSnapshot> StatusSnapshots { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite($"Data Source=status.db");
    }
}