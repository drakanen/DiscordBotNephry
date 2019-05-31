using System.Threading.Tasks;

using Discord.Commands;

namespace DiscordBot.Core.Commands
{
    public class Owner : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Displays information on the owner of the bot and how to contact him (me)
        /// </summary>
        [Command("owner"), Alias("contact"), Summary("Displays the owner's Discord tag and mentions him.")]
        public async Task OwnerInfo()
        {
            await Context.Channel.SendMessageAsync("The owner and creator of this bot is Drakanen#2739 (<@!187644003091480577>)." +
                " If you have any questions or concerns, please contact him.");
        }
    }
}