using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace DiscordBot.Resources.Database
{
    public class SqliteDbContext : DbContext
    {
        //Set of database tables
        public DbSet<Gold> Gold { get; set; }
        public DbSet<Warning> Warnings { get; set; }
        public DbSet<GuildLocationSettings> GuildLocationSettings { get; set; }
        public DbSet<BannedWords> BannedWords { get; set; }
        public DbSet<CustomCommands> CustomCommands { get; set; }

        /// <summary>
        /// Retrieves the database.
        /// </summary>
        /// <param name="Options">Used to configure the database.</param>
        protected override void OnConfiguring(DbContextOptionsBuilder Options)
        {
            string DbLocation = Assembly.GetEntryAssembly().Location.Replace(@"bin\Debug\netcoreapp2.1\DiscordBot.dll", @"Data\");
            Options.UseSqlite($"Data Source={DbLocation}Database.sqlite");
        }
    }
}
