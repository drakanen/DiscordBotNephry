using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace DiscordBot.Resources.Database
{
    public class GuildLocationSettings
    {
        /// <summary>
        /// Database table that holds information about the guild.
        /// </summary>
        [Key]
        public ulong Serverid { get; set; } //Primary key
        public string ServerName { get; set; }
        public ulong WelcomeChannel { get; set; }
        public string WelcomeMessage { get; set; }
        public ulong BotSpamChannel { get; set; }
        public ulong ChatLogChannel { get; set; }
        public ulong ModLogChannel { get; set; }
        public int GoldInterest { get; set; }
    }
}