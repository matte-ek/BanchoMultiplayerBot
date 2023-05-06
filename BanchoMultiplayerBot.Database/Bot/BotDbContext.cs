﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BanchoMultiplayerBot.Database.Bot.Models;
using Microsoft.EntityFrameworkCore;

namespace BanchoMultiplayerBot.Database.Bot
{
    public class BotDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite($"Data Source=bot.db");
    }
}