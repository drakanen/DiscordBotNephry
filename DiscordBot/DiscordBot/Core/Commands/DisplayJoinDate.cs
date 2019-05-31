using System;
using System.Threading.Tasks;

using Discord.Commands;

namespace DiscordBot.Core.Commands
{
    public class DisplayJoinDate : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Displays the time and date in EST that the user joined the server
        /// </summary>
        [Command("joindate"), Summary("Display the date the user joined the server")]
        public async Task GetJoinDate()
        {
            //Get eastern time zone
            TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

            //Get the date joined
            DateTimeOffset? dayJoined = Context.Guild.GetUser(Context.User.Id).JoinedAt;

            //Convert dayJoined to EST                  Do not convert to UTC
            DateTime joinDate = TimeZoneInfo.ConvertTimeFromUtc(dayJoined.Value.DateTime, easternZone);

            //Display join date
            await Context.Channel.SendMessageAsync($"{Context.User.Mention} joined on {joinDate} EST");
        }
    }
}
