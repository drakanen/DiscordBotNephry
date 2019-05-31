using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace DiscordBot.Resources.Database
{
    public class Gold
    {
        /// <summary>
        /// Database table that holds information about gold.
        /// </summary>
        [Key]
        public int UniqueNumber { get; set; } //Primary key
        public ulong Serverid { get; set; } //Foreign key
        public ulong UserId { get; set; } //Foreign key
        public string Username { get; set; }
        public int Amount { get; set; }
    }
}
