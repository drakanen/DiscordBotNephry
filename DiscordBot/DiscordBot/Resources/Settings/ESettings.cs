using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Resources.Settings
{
    public static class ESettings
    {
        //Gives the bot information.
        public static string token;
        public static ulong owner;
        public static List<ulong> log;
        public static string version;
        public static List<ulong> banned;
    }
}
