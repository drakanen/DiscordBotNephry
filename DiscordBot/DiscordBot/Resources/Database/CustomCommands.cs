using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace DiscordBot.Resources.Database
{
    public class CustomCommands
    {
        /// <summary>
        /// Database table that holds information about the custom commands.
        /// </summary>
        [Key]
        public int UniqueNumber { get; set; } //Primary key
        public ulong Serverid { get; set; } //Foreign key
        public string Destination { get; set; }
        public string CommandName { get; set; }
        public string Command { get; set; }
        public string CommandDescription { get; set; }
    }
}
