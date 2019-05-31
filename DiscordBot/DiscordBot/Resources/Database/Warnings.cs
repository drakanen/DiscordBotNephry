using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace DiscordBot.Resources.Database
{
    public class Warning
    {
        /// <summary>
        /// Database table containing information about warnings.
        /// </summary>
        [Key]
        public int UniqueNumber { get; set; } //Primary key
        public ulong Serverid { get; set; } //Foreign key
        public ulong UserId { get; set; }   //Foreign key
        public string Username { get; set; }
        public int AmountOfWarnings { get; set; }
    }
}
