using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace DiscordBot.Resources.Database
{
    public class BannedWords
    {
        /// <summary>
        /// Database table that holds information about banned words.
        /// </summary>
        [Key]
        public int UniqueNumber { get; set; } //Primary key
        public ulong Serverid { get; set; } //Foreign key
        public string Word { get; set; }
    }
}
