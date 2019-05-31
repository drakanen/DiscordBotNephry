using System;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Core.Moderation;
using DiscordBot.Resources.Database;
using System.Linq;
using System.Collections.Concurrent;

namespace DiscordBot.Core.Currency
{
    public class Golds : ModuleBase<SocketCommandContext>
    {
        private static readonly ConcurrentDictionary<long, DateTimeOffset> goldLastCall = new ConcurrentDictionary<long, DateTimeOffset>();

        /// <summary>
        /// Displays how much hold the target user has, or yourself if you don't target anyone
        /// </summary>
        /// <param name="target">The user to show the gold of</param>
        [Command("gold"), Alias("currency"), Summary("Shows all your current gold")]
        public async Task CheckGold(IUser target = null)
        {
            //Cool down timer variables
            ulong id = Context.User.Id; //Holds the user's ID
            DateTimeOffset nowUtc = DateTimeOffset.UtcNow; //Gets the current time
            bool canReturnGold = true; //Boolean for if cooldown has expired

            //Cool down timer
            goldLastCall.AddOrUpdate((long) id, nowUtc, (key, oldValue) =>
            {
                TimeSpan elapsed = nowUtc - oldValue;
                if (elapsed.TotalSeconds < 5) //If more than 5 seconds has past, allow the command to go
                {
                    canReturnGold = false;
                    return oldValue;
                }
                return nowUtc;
            });

            //If cooldown has not expired
            if (!canReturnGold)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you are on cooldown.");
                return;
            }

            if (target == null) //Targeting yourself
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you have {Data.Data.GetGold(Context.User.Id, Context.Guild.Id)} gold!");
            }
            else if (!target.IsBot) //Targeting someone else
                await Context.Channel.SendMessageAsync($"{target.Mention} has {Data.Data.GetGold(target.Id, Context.Guild.Id)} gold!");
            else if (target.IsBot) //Targeting a bot
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, I am a bot. I have no need for gold.");
            }
        }

        /// <summary>
        /// Gives gold to someone without taking any away from the user. Requires the administrator permission.
        /// </summary>
        /// <param name="target">The user to give gold to.</param>
        /// <param name="Amount">How much gold to give.</param>
        /// <param name="reason">Why they are giving them gold for the mod log.</param>
        [Command("admingive"), Alias("admingift"), Summary("Used to give people gold")]
        public async Task AdminGive(IUser target = null, int Amount = 0, string reason = "No reason provided.")
        {
            //Make sure someone was targeted
            if (target == null)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you didn't mention a user to give the gold to! Please use this syntax: !gold give @user amount reason");
                return;
            }

            //At this point, we made sure that a user has been pinged
            if (target.IsBot)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, bots can't use gold, so you can't give gold to a bot!");
                return;
            }

            //At this point we made sure a user has been pinged AND that the user is not a bot
            if (Amount == 0)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you need to specify a valid amount of gold that I need to give to {target.Mention}");
                return;
            }

            //Make sure the value is above 0
            if (Amount < 1)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you can only give a positive value in gold.");
                return;
            }

            //At this point, we made sure a user has been pinged, that the user is not a bot AND that there is a valid amount of coins
            SocketGuildUser User1 = Context.User as SocketGuildUser;
            if (!User1.GuildPermissions.Administrator)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you don't have administrator permissions in this discord server!");
                return;
            }

            await Data.Data.SaveGold(target.Id, Amount, Context.Guild.Id, target.Username);
            await ModLog.PostInModLog(Context.Guild, "Gave Gold", Context.User, target as IGuildUser, reason);

            await Context.Channel.SendMessageAsync($"tada: {target.Mention} you have received {Amount} gold from {Context.User.Mention}!");
        }

        /// <summary>
        /// Gives gold to someone while taking that gold away from the user. The normal way of giving gold.
        /// </summary>
        /// <param name="target">Who to give the gold to.</param>
        /// <param name="Amount">The amount of gold to give.</param>
        /// <returns></returns>
        [Command("give"), Alias("gift"), Summary("Used to give people gold, takes away from giver")]
        public async Task Give(IUser target = null, int Amount = 0)
        {
            //Make sure someone is targeted
            if (target == null)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you didn't mention a user to give the gold to! Please use this syntax: !gold give @user amount");
                return;
            }

            //At this point, we made sure that a user has been pinged
            if (target.IsBot)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, bots can't use gold, so you can't give gold to a bot!");
                return;
            }

            //At this point we made sure a user has been pinged AND that the user is not a bot
            if (Amount == 0)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you need to specify a valid amount of gold that I need to give to {target.Mention}");
                return;
            }

            //Make sure the value is above 0
            if (Amount < 1)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you can only give a positive value in gold.");
                return;
            }

            if (Data.Data.GetGold(Context.User.Id, Context.Guild.Id) < Amount)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you cannot give more money than you currently have!");
                return;
            }

            try
            {
                //Get the interest level and remove it from the given amount
                double newamount = (double) Amount * ((double) Data.Data.GetInterest(Context.Guild.Id) / 100);
                Amount -= (int)Math.Round(newamount, MidpointRounding.AwayFromZero);
            }
            catch (DivideByZeroException) { }

            //Take gold away from the giver and add it to the reciever
            await Data.Data.SaveGoldMinus(Context.User.Id, Amount, Context.Guild.Id, Context.User.Username);
            await Data.Data.SaveGold(target.Id, Amount, Context.Guild.Id, target.Username);

            await Context.Channel.SendMessageAsync($"Tada: {target.Mention} you have received {Amount} gold from {Context.User.Mention}!");
        }

        /// <summary>
        /// Takes gold away from the target'd user.
        /// </summary>
        /// <param name="target">Who to take gold away from.</param>
        /// <param name="Amount">The amount to take.</param>
        /// <param name="reason">The reason for taking it to be used in the mod log.</param>
        [Command("take"), Alias("bring"), Summary("Take away some gold from the user")]
        public async Task Take(IUser target = null, int Amount = 0, string reason = "No reason provided.")
        {
            //Make sure the command issuer is an administrator
            SocketGuildUser User1 = Context.User as SocketGuildUser;
            if (!User1.GuildPermissions.Administrator)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you don't have administrator permissions in this discord server! Ask a moderator or the owner to execute this command!");
                return;
            }

            //Make sure a user was pinged
            if (target == null)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you didn't mention a user to give the gold to! Please use this syntax: !gold give @user amount reason");
                return;
            }

            //Make sure the user pinged is not a bot
            if (target.IsBot)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, bots can't use gold, so you can't give gold to a bot!");
                return;
            }

            //Make sure an amount is specified
            if (Amount == 0)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you need to specify a valid amount of gold that I need to give to {target.Mention}");
                return;
            }

            //Make sure the value is above 0
            if (Amount > 0)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you can only take a positive value in gold.");
                return;
            }

            //Make sure the user has enough gold to take
            if (Amount > Data.Data.GetGold(target.Id, Context.Guild.Id))
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, {target.Mention} does not have that much gold!" +
                    $" If you want to take all of their gold use the !reset command.");
                return;
            }

            await Context.Channel.SendMessageAsync($"tada: {Context.User.Mention} you have taken {Amount} gold from {target.Mention}!");

            //Take gold away and post admin action in mod log
            await Data.Data.SaveGoldMinus(target.Id, Amount, Context.Guild.Id, target.Username);
            await ModLog.PostInModLog(Context.Guild, "Took Gold", Context.User, target as IGuildUser, reason);
        }

        /// <summary>
        /// Resets the target'd users gold back to 0. Requires the administrator permission.
        /// </summary>
        /// <param name="target">The person to reset.</param>
        /// <param name="reason">The reason you reset them to be displayed in the mod log.</param>
        /// <returns></returns>
        [Command("reset"), Summary("Resets the user's entire progress")]
        public async Task Reset(IUser target = null, string reason = "No reason provided.")
        {
            SocketGuildUser User1 = Context.User as SocketGuildUser;
            if (!User1.GuildPermissions.Administrator) //Make sure user issuing command is admin
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you don't have administrator permissions in this discord server! Ask a administrator or the owner to execute this command!");
                return;
            }

            if (target == null) //Make sure a user was pinged
            {
                await Context.Channel.SendMessageAsync($"You need to tell me which user you want to reset the gold of! For example: !gold reset {Context.User.Mention}");
                return;
            }
                
            if (target.IsBot) //Make sure pinged user isn't a bot
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, bots can't use gold, so you also can't reset the progress of bots! :robot:");
                return;
            }
            await Context.Channel.SendMessageAsync($"{target.Mention}, you have been reset by {Context.User.Mention}! This means you have lost all your gold!");
            await ModLog.PostInModLog(Context.Guild, "Reset Gold", Context.User, target as IGuildUser, reason);

            //Remove the gold
            using (SqliteDbContext DBContext = new SqliteDbContext())
            {
                DBContext.Gold.RemoveRange(DBContext.Gold.Where(x => x.UserId == target.Id && x.Serverid == Context.Guild.Id));
                await DBContext.SaveChangesAsync();
            }
        }
        
        /// <summary>
        /// Displays the top ten users with the most gold in the guild. Sends it in a DM as an embed.
        /// </summary>
        [Command("mostgold"), Alias("topgold", "richboys", "topten"), Summary("Shows the top 10 people with the most gold in the guild")]
        public async Task ListUsersWithMostGold()
        {
            Gold[] goldList = Data.Data.GetTopTenGold(Context.Guild.Id);
            string list = "";
            foreach (Gold gold in goldList)
            {
                list += gold.Username + " - " + gold.Amount + "\n";
            }

            //Embedbuilder object
            EmbedBuilder Embed = new EmbedBuilder();

            //Assign the author
            Embed.WithAuthor("Top 10 Users By Gold", Context.User.GetAvatarUrl());

            //Assign the color on the left side
            Embed.WithColor(40, 200, 150);

            //Create the description
            Embed.WithDescription(list);
            await Context.User.SendMessageAsync("", false, Embed.Build());
        }
    }
}
