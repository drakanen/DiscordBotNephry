using System;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace DiscordBot.Core.Currency
{
    public class Store : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// A store holding items you can buy with gold.
        /// It currently holds:
        /// Change your nickname for 100 gold.
        /// Change someone else's nickname for 200 gold.
        /// Get the "richboi" role for 10000 gold.
        /// Use the bot to mention everyone for 50000 gold.
        /// </summary>
        [Command("store"), Alias("shop"), Summary("A shop menu containing information on items you can buy")]
        public async Task DisplayStore()
        {
            await Context.User.SendMessageAsync($"Currently available store items:\n" +
                $"\"Change Nickname\" Changes your nickname for 100 gold - !nickname name.\n" +
                $"\"Change Target's Nickname\" Changes the targets nickname for 200 gold - !nicknametarget @target name" +
                $"\"Rich Boi\" role for 10,000 gold - !richboi\n" +
                $"You get to use the bot to mention everyone for 50,000 gold - !mentioneveryone\n\n" +
                $"Note: These are options available in every server, your server might have custom options to purchase.\n" +
                $"Make sure you check the `!getcustomcommands` menu to see if there are any.");
        }

        /// <summary>
        /// Change your nickname in the guild
        /// </summary>
        /// <param name="name">The name to change it to.</param>
        [Command("nickname"), Summary("Change your nickname for 100 gold")]
        public async Task ChangeMyName([Remainder] string name)
        {
            const int cost = 100; //Cost to change your nickname

            //Make sure they have enough gold
            if (Data.Data.GetGold(Context.User.Id, Context.Guild.Id) < cost)
            {
                await Context.Channel.SendMessageAsync($"Sorry {Context.User.Mention}, you do not have enough gold to change your nickname :frowning: ");
                return;
            }

            //Make sure the name they want to change it to does not contain a banned word
            foreach (string word in Data.Data.GetBannedWords(Context.Guild.Id))
            {
                if (name.ToLower().Contains(word))
                {
                    await Context.Channel.SendMessageAsync($"{Context.User.Mention} your nickname cannot have banned words in it!");
                    return;
                }
            }
            //Get the user as a SocketGuildUser
            SocketGuildUser userToChange = Context.User as SocketGuildUser;
            try
            {   //Change their nickname
                await userToChange.ModifyAsync(x => x.Nickname = name);
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, your nickname is now \"{name}\" :grinning: ");
                await Data.Data.SaveGoldMinus(Context.User.Id, cost, Context.Guild.Id, Context.User.Username);
            }
            catch (Exception)
            {
                await Context.Channel.SendMessageAsync("My role is not above the current user's highest role. Please move me higher :grinning:");
            }
        }

        /// <summary>
        /// Change someone else's nickname in the guild.
        /// </summary>
        /// <param name="target">Whose name to change</param>
        /// <param name="name">The name to change it to.</param>
        [Command("nicknametarget"), Summary("Change someone else's nickname for 200 gold")]
        public async Task ChangeSomeonesName(SocketGuildUser target, [Remainder] string name)
        {
            const int cost = 200; //Cost to use this command

            //Make sure the user has enough gold to use the command
            if (Data.Data.GetGold(Context.User.Id, Context.Guild.Id) < cost)
            {
                await Context.Channel.SendMessageAsync($"Sorry {Context.User.Mention}, you do not have enough gold to change someones nickname :frowning: ");
                return;
            }

            //Make sure the name does not contain a banned word
            foreach (string word in Data.Data.GetBannedWords(Context.Guild.Id))
            {
                if (name.ToLower().Contains(word))
                {
                    await Context.Channel.SendMessageAsync($"{Context.User.Mention} you cannot change someones nickname to have a banned word in it!");
                    return;
                }
            }

            try
            {
                await target.ModifyAsync(x => x.Nickname = name); //Change their nickname
                await Context.Channel.SendMessageAsync($"{target.Mention}, {Context.User.Mention} has paid 200 gold to change your nickname! Enjoy your new name :grinning: ");
                await Data.Data.SaveGoldMinus(Context.User.Id, cost, Context.Guild.Id, Context.User.Username); //Take away the gold
            }
            catch (Exception)
            {
                await Context.Channel.SendMessageAsync("My role is not above the target user's highest role. Please move me higher :grinning:");
            }
        }

        /// <summary>
        /// Gives the user the "richboi" role. If it doesn't exist it is created.
        /// </summary>
        [Command("richboi"), Summary("Buy the richboi role for 10,000 gold")]
        public async Task AssignRichBoi()
        {
            const int cost = 10000; //Cost of the command

            //Make sure they have enough gold
            if (Data.Data.GetGold(Context.User.Id, Context.Guild.Id) < cost)
            {
                await Context.Channel.SendMessageAsync($"Sorry {Context.User.Username}, you do not have enough gold to purchase the \"Rich Boi\" role.");
                return;
            }

            //Find the Rich Boi role and add it
            foreach (SocketRole roles in Context.Guild.Roles)
            {
                if (roles.Name == "Rich Boi")
                {
                    await (Context.User as IGuildUser).AddRoleAsync(roles); //Add the role
                    await Context.Channel.SendMessageAsync($"{Context.User.Username} has bought the \"Rich Boi\" role!");
                    await Data.Data.SaveGoldMinus(Context.User.Id, cost, Context.Guild.Id, Context.User.Username); //Take away the cost of the command
                    return;
                }
            }

            //If the role does not exist, create it
            GuildPermissions pm = new GuildPermissions(false, false, false, false, false, false, true, false, true, true, false, false, true, true, true, false,
                                                        true, true, true, false, false, false, true, false, true, false, false, false, false);
            await Context.Guild.CreateRoleAsync("Rich Boi", pm, new Color(255, 167, 66)); //Create the role
            await AssignRichBoi(); //Recursively call this method again, if bot doesn't have permissions to create the role it'll get an exception and exit on its own
        }

        /// <summary>
        /// Has the bot mention everyone in the guild.
        /// </summary>
        [Command("mentioneveryone"), Summary("Use the bot to mention everyone!")]
        public async Task MentionEveryone()
        {
            const int cost = 50000; //Cost to do this
            
            //Make sure user has enough gold to do this
            if (Data.Data.GetGold(Context.User.Id, Context.Guild.Id) < cost)
            {
                await Context.Channel.SendMessageAsync($"Sorry {Context.User.Username}, you do not have enough gold to purchase the \"Rich Boi\" role.");
                return;
            }

            //Mention everyone
            await Context.Channel.SendMessageAsync($"{Context.Guild.EveryoneRole.Mention} Attention all users, you have been mentioned!");
            await Data.Data.SaveGoldMinus(Context.User.Id, cost, Context.Guild.Id, Context.User.Username); //Take away the cost
        }
    }
}