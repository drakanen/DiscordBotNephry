using System;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using DiscordBot.Core.Moderation;

namespace DiscordBot.Core.GuildSettings
{
    public class GuildInformation : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Sets the welcome channel in the database.
        /// </summary>
        /// <param name="channel">The channel to set it to.</param>
        [Command("welcomechannel"), Summary("Set the welcome channel to welcome new users in")]
        public async Task SetWelcomeChannel(IGuildChannel channel)
        {
            SocketGuildUser checkUser = Context.User as SocketGuildUser;
            if (!checkUser.GuildPermissions.Administrator)
            {
                await Context.Channel.SendMessageAsync($"Only an administrator can set the welcome channel.");
                return;
            }

            //Save the channel info
            await Data.Data.SetWelcomeChannel(Context.Guild.Id, channel.Id, Context.Guild.Name);
            await Context.Channel.SendMessageAsync($"Welcome channel set to \"{channel.Name}\".");

            //Channel to send the mod logs in
            await ModLog.PostInModLog(Context.Guild, "Set the welcome channel to: " + channel.Name, Context.User, null, "");
        }

        /// <summary>
        /// Gets the welcome channel fromn the database and displays it in chat.
        /// </summary>
        [Command("welcomechannel"), Summary("Get the welcome channel")]
        public async Task GetWelcomeChannel()
        {
            if (Data.Data.GetWelcomeChannel(Context.Guild.Id) != 0)
                await Context.Channel.SendMessageAsync($"The welcome channel is currently: <#{Data.Data.GetWelcomeChannel(Context.Guild.Id)}>");
            else
                await Context.Channel.SendMessageAsync($"There is not currently a welcome channel assigned. Please assign one with \n`!welcomechannel #channel`");
        }

        /// <summary>
        /// Sets the welcome message in the database.
        /// </summary>
        /// <param name="message">The message to set it to.</param>
        [Command("welcomemessage"), Summary("Set the welcome message to display in the welcome channel when someone joins")]
        public async Task SetWelcomeMessage([Remainder]string message)
        {
            SocketGuildUser checkUser = Context.User as SocketGuildUser;
            if (!checkUser.GuildPermissions.Administrator)
            {
                await Context.Channel.SendMessageAsync($"Only an administrator can set the welcome message.");
                return;
            }
            
            await Data.Data.SetWelcomeMessage(Context.Guild.Id, message, Context.Guild.Name);
            await Context.Channel.SendMessageAsync($"Welcome message set to \"{message}\".");

            //Channel to send the mod logs in
            await ModLog.PostInModLog(Context.Guild, $"Set the welcome message to: \"{message}\"", Context.User, null, "");
        }

        /// <summary>
        /// Gets the welcome message and displays it in chat.
        /// </summary>
        [Command("welcomemessage"), Summary("Get the welcome message")]
        public async Task GetWelcomeMessage()
        {
            await Context.Channel.SendMessageAsync($"The welcome message is currently: {Data.Data.GetWelcomeMessage(Context.Guild.Id)}" +
                $"If you would like to change it, please use \n`!welcomemessage message`");
        }

        /// <summary>
        /// Set the bot spam channel ID in the database.
        /// </summary>
        /// <param name="channel">The channel to set it to.</param>
        [Command("botspamchannel"), Summary("Set the bot spam channel so all bot commands must be in that channel except for moderator+")]
        public async Task SetBotSpamChannel(IGuildChannel channel)
        {
            SocketGuildUser checkUser = Context.User as SocketGuildUser;
            if (!checkUser.GuildPermissions.Administrator)
            {
                await Context.Channel.SendMessageAsync($"Only an administrator can set the bot spam channel.");
                return;
            }

            //Set the channel
            await Data.Data.SetBotSpamChannel(Context.Guild.Id, channel.Id, Context.Guild.Name);
            await Context.Channel.SendMessageAsync($"Bot spam channel successful set to \"{channel.Name}\"");

            //Channel to send the mod logs in
            await ModLog.PostInModLog(Context.Guild, "Set the bot spam channel to: " + channel.Name, Context.User, null, "");
        }
        
        /// <summary>
        /// Gets the bot spam channel from the database and displays it in the channel.
        /// </summary>
        [Command("botspamchannel"), Summary("Get the bot spam channel")]
        public async Task GetBotSpamChannel()
        {
            if (Data.Data.GetBotSpamChannel(Context.Guild.Id) != 0)
                await Context.Channel.SendMessageAsync($"The bot spam channel is currently: <#{Data.Data.GetBotSpamChannel(Context.Guild.Id)}>");
            else
                await Context.Channel.SendMessageAsync("This server has no bot spam channel. Please assign one with \n`!botspamchannel #channel`");
        }

        /// <summary>
        /// Sets the chat log channel in the database.
        /// </summary>
        /// <param name="channel">The channel to set it to.</param>
        [Command("chatlogchannel"), Summary("set the chat log channel")]
        public async Task SetChatLogChannel(IGuildChannel channel)
        {
            SocketGuildUser checkUser = Context.User as SocketGuildUser;
            if (!checkUser.GuildPermissions.Administrator)
            {
                await Context.Channel.SendMessageAsync($"Only an administrator can set the chat log channel.");
                return;
            }

            await Data.Data.SetChatLogChannel(Context.Guild.Id, channel.Id, Context.Guild.Name);

            //Send a test message
            ulong channelToSendMessageTo = Data.Data.GetChatLogChannel(Context.Guild.Id);
            SocketGuildChannel channelToGet = Context.Guild.GetChannel(channelToSendMessageTo);
            ISocketMessageChannel channelToSendTestMessage = channelToGet as ISocketMessageChannel;
            try
            {
                await channelToSendTestMessage.SendMessageAsync("Test.");
            }
            catch(Exception)
            {
                await Context.Channel.SendMessageAsync($"I cannot send messages to the chat log channel. " +
                    $"Please make sure I have permissions to send messages in <#{channel.Id}> then try again.");
                await Data.Data.SetChatLogChannel(Context.Guild.Id, 0, Context.Guild.Name);
                return;
            }


            await Context.Channel.SendMessageAsync($"Chat log channel successfully set to <#{channel.Id}>");
            //Channel to send the mod logs in
            await ModLog.PostInModLog(Context.Guild, $"Set the chat log channel to: <#{channel.Id}>", Context.User, null, "");
        }

        /// <summary>
        /// Gets the chat log channel from the database and displays it in chat.
        /// </summary>
        /// <returns></returns>
        [Command("chatlogchannel"), Summary("Get the chat log channel")]
        public async Task GetChatLogChannel()
        {
            SocketGuildUser User1 = Context.User as SocketGuildUser;
            if (!User1.GuildPermissions.Administrator)
            {
                return;
            }

            if (Data.Data.GetChatLogChannel(Context.Guild.Id) != 0)
                await Context.Channel.SendMessageAsync($"<#{Data.Data.GetChatLogChannel(Context.Guild.Id)}>");
            else
                await Context.Channel.SendMessageAsync("There is no chat log channel in this server. Please assign one with \n`!chatlogchannel #channel`");
        }
        
        /// <summary>
        /// Stops the chat log from being posted in by setting it to 0 in the database.
        /// </summary>
        [Command("stopchatlog"), Summary("Stop the chat log from being posted in")]
        public async Task StopChatLog()
        {
            SocketGuildUser checkUser = Context.User as SocketGuildUser;
            if (!checkUser.GuildPermissions.Administrator)
            {
                await Context.Channel.SendMessageAsync($"Only an administrator can stop the chat log.");
                return;
            }

            await Data.Data.SetChatLogChannel(Context.Guild.Id, 0, Context.Guild.Name);
            await Context.Channel.SendMessageAsync($"{Context.User.Mention}, I will stop posting chat logs now!");

            //Channel to send the mod logs in
            await ModLog.PostInModLog(Context.Guild, "Stopped the chat log.", Context.User, null, "");
        }

        /// <summary>
        /// Sets the mod log channel in the database.
        /// </summary>
        /// <param name="channel">The channel to set it to</param>
        [Command("modlogchannel"), Summary("Set the mod log channel to post bot admin activities in")]
        public async Task SetModLogChannel(IGuildChannel channel)
        {
            SocketGuildUser checkUser = Context.User as SocketGuildUser;
            if (!checkUser.GuildPermissions.Administrator)
            {
                await Context.Channel.SendMessageAsync($"Only an administrator can set the mod log channel.");
                return;
            }

            //Set the channel
            await Data.Data.SetModLogChannel(Context.Guild.Id, channel.Id, Context.Guild.Name);
            
            //Send a test message
            SocketUser Nephry = Context.Guild.GetUser(322806920203337740);
            try
            {
                await ModLog.PostInModLog(Context.Guild, "Test", Nephry, null, "Testing");
            }
            catch(Exception)
            {
                await Data.Data.SetModLogChannel(Context.Guild.Id, 0, Context.Guild.Name);
                return;
            }
            await Context.Channel.SendMessageAsync($"Mod log channel successfully set to \"{channel.Name}\"");

            //Channel to send the mod logs in
            await ModLog.PostInModLog(Context.Guild, "Set the mod log channel to: " + channel.Name, Context.User, null, "");
        }

        /// <summary>
        /// Gets the mod log channel from the database and posts it in the channel.
        /// </summary>
        [Command("modlogchannel"), Summary("Get the mod log channel")]
        public async Task GetModLogChannel()
        {
            SocketGuildUser User1 = Context.User as SocketGuildUser;
            if (!User1.GuildPermissions.Administrator)
            {
                return;
            }
            if (Data.Data.GetModLogChannel(Context.Guild.Id) != 0)
                await Context.Channel.SendMessageAsync($"<#{Data.Data.GetModLogChannel(Context.Guild.Id)}>");
            else
                await Context.Channel.SendMessageAsync("There is no mod log channel in this server. Please assign one with \n`!modlogchannel #channel`");
        }

        /// <summary>
        /// Stops the mod log from being posted in by changing it to a 0 in the database.
        /// </summary>
        [Command("stopmodlog"), Summary("Stop the bot from posting in the mod log channel")]
        public async Task StopModLog()
        {
            SocketGuildUser checkUser = Context.User as SocketGuildUser;
            if (!checkUser.GuildPermissions.Administrator)
            {
                await Context.Channel.SendMessageAsync($"Only an administrator can stop the bot from posting in the mod log channel.");
                return;
            }

            await Data.Data.SetModLogChannel(Context.Guild.Id, 0, Context.Guild.Name);
            await Context.Channel.SendMessageAsync($"{Context.User.Mention}, I will stop posting mod logs now!");

            //Channel to send the mod logs in
            await ModLog.PostInModLog(Context.Guild, "Stopped the mod log channel.", Context.User, null, "");
        }

        /// <summary>
        /// Stops the bot from only reading and responding in the bot spam channel.
        /// Sets the value for it to 0 in the database.
        /// </summary>
        [Command("freebot"), Summary("Remove the bot spam channel")]
        public async Task NoBotSpamChannel()
        {
            SocketGuildUser checkUser = Context.User as SocketGuildUser;
            if (!checkUser.GuildPermissions.Administrator)
            {
                await Context.Channel.SendMessageAsync($"Only an administrator can free the bot.");
                return;
            }

            await Data.Data.SetBotSpamChannel(Context.Guild.Id, 0, Context.Guild.Name);
            await Context.Channel.SendMessageAsync($"I can now respond freely in all channels.");

            //Channel to send the mod logs in
            await ModLog.PostInModLog(Context.Guild, "Freed the bot.", Context.User, null, "");
        }

        /// <summary>
        /// Sets the interest rate in the database.
        /// </summary>
        /// <param name="rate">The rate to set it to.</param>
        /// <returns></returns>
        [Command("interest"), Summary("Set the interest rate for giving gold to others as a percentage")]
        public async Task SetInterest(double rate)
        {
            SocketGuildUser checkUser = Context.User as SocketGuildUser;
            if (!checkUser.GuildPermissions.Administrator)
            {
                await Context.Channel.SendMessageAsync($"Only an administrator can set the interest rate.");
                return;
            }

            try
            {
                await Data.Data.SetInterest(Context.Guild.Id, Context.Guild.Name,(int) rate);
            }
            catch(Exception)
            {
                await Context.Channel.SendMessageAsync("Please make sure the interest rate is in whole value format, 5% would be \"!setinterest 5\".");
                return;
            }
            await Context.Channel.SendMessageAsync($"Interest rate has been set to {rate}%.");

            //Channel to send the mod logs in
            await ModLog.PostInModLog(Context.Guild, $"Set interest rate to {rate}%", Context.User, null, "");
        }

        /// <summary>
        /// Gets the interest rate from the database and posts it in chat.
        /// </summary>
        [Command("interest"), Summary("Get the interst rate")]
        public async Task GetInterest()
        {
            await Context.Channel.SendMessageAsync($"Interest rate: {((double) Data.Data.GetInterest(Context.Guild.Id)).ToString()}%");
        }
    }
}