using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Resources.Datatypes
{
    public class Setting
    {
        //Holds information for and about the bot
        public string Token { get; set; }
        public ulong Owner { get; set; }
        public List<ulong> Log { get; set; }
        public string Version { get; set; }
        public List<ulong> Banned { get; set; }
    }
}
