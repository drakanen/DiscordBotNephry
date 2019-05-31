using System;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

using Discord;
using Discord.WebSocket;
using Discord.Commands;

using DiscordBot.Core.Data;
using DiscordBot.Core.Moderation;
using DiscordBot.Core.GuildSettings;
using DiscordBot.Resources.Settings;
using DiscordBot.Resources.Datatypes;

using Newtonsoft.Json;
using System.Collections.Generic;

namespace DiscordBot
{
    class Program
    {
        private DiscordSocketClient Client; //Necessary
        private CommandService Commands;
        private DateTime timeUtc; //Getting the current time for the chat log
        private readonly TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"); //For converting times in the chat log
        private DateTime today; //Get the date for the chat log

        /// <summary>
        /// Starts the bot.
        /// </summary>
        /// <param name="args">Required.</param>
        static void Main(string[] args)
        => new Program().MainAsync().GetAwaiter().GetResult();

        /// <summary>
        /// Controls the bot.
        /// </summary>
        private async Task MainAsync()
        {
            string JSON = "";
            string SettingsLocation = ""; //Settings location
            SettingsLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location).Replace(@"bin\Debug\netcoreapp2.1", @"Core\Data\Settings.json");
            using (FileStream Stream = new FileStream(SettingsLocation, FileMode.Open, FileAccess.Read))
            using (StreamReader ReadSettings = new StreamReader(Stream))
            {
                JSON = ReadSettings.ReadToEnd(); //Read the settings
            }

            //Get the settings
            Setting Settings = JsonConvert.DeserializeObject<Setting>(JSON);
            ESettings.banned = Settings.Banned;
            ESettings.log = Settings.Log;
            ESettings.owner = Settings.Owner;
            ESettings.token = Settings.Token;
            ESettings.version = Settings.Version;

            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info, //Set the log recording level
                MessageCacheSize = 100
            });

            Commands = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,   //Set the case sensitivity level
                DefaultRunMode = RunMode.Async, //Set the run mode
                LogLevel = LogSeverity.Debug    //Set the log recording level
            });

            Client.MessageReceived += Client_MessageReceived; //Called whenever someone types a message
            Client.MessageUpdated += Client_MessageUpdated; //Called whenever a message is updated
            Client.UserJoined += Client_UserJoin;            //Called whenever someone joins the server
            Client.UserLeft += Client_Left; //Called whenever someone leaves the server
            Client.Ready += Client_Ready; //Mark client as ready
            Client.Log += Client_Log; //Write the log
            Client.JoinedGuild += Client_JoinGuild; //Called when the bot joins the guild

            await Commands.AddModulesAsync(Assembly.GetEntryAssembly(), null); //Add the commands

            //Log into the bot
            await Client.LoginAsync(TokenType.Bot, ESettings.token);
            await Client.StartAsync(); //Start the bot

            await Task.Delay(-1);
        }

        /// <summary>
        /// Called when a user leaves the guild. Displays a message saying the user has left.
        /// </summary>
        /// <param name="user">The user who left the guild.</param>
        private async Task Client_Left(SocketGuildUser user)
        {
            SocketTextChannel channel = Client.GetChannel(Data.GetWelcomeChannel(user.Guild.Id)) as SocketTextChannel;
            await channel.SendMessageAsync($"{user.Mention} has left the server. :cry: \n");
        }

        /// <summary>
        /// Called when the bot joins a guild. Sends a message saying the bot is here.
        /// </summary>
        /// <param name="guild">The guild the bot joined.</param>
        private async Task Client_JoinGuild(SocketGuild guild)
        {
            SocketTextChannel channel = guild.DefaultChannel;
            await channel.SendMessageAsync("I am here now! :grinning:");
        }

        /// <summary>
        /// Keeps a log in the specified channel.
        /// </summary>
        /// <param name="Message">The log message to send.</param>
        private async Task Client_Log(LogMessage Message)
        {
            Console.WriteLine($"{DateTime.Now} at {Message.Source} {Message.Message}");
            try
            {
                SocketGuild Guild = Client.Guilds.Where(x => x.Id == ESettings.log[0]).FirstOrDefault();
                SocketTextChannel Channel = Guild.Channels.Where(x => x.Id == ESettings.log[1]).FirstOrDefault() as SocketTextChannel;
                await Channel.SendMessageAsync($"{DateTime.Now} at {Message.Source} | {Message.Message}");
            }
            catch { }
        }

        /// <summary>
        /// Sets the bot's status when brought online.
        /// </summary>
        private async Task Client_Ready()
        {
            await Client.SetGameAsync("!help", null, ActivityType.Playing); //Sets the game the bot is playing
            
        }

        /// <summary>
        /// Called when a user joins the server. Sends the guild's welcome message.
        /// </summary>
        /// <param name="user">The user who joined the guild.</param>
        public async Task Client_UserJoin(SocketGuildUser user)
        {
            //Get the channel to send the message to
            SocketTextChannel channel = Client.GetChannel(Data.GetWelcomeChannel(user.Guild.Id)) as SocketTextChannel;
            try
            {
                string message = Data.GetWelcomeMessage(user.Guild.Id); //Get the welcome message

                if (message.Contains("@newuser")) //Parse for keyword
                {
                    message = message.Replace("@newuser", user.Mention); //Mention the user that joined
                }

                message += "\n";
                await channel.SendMessageAsync(message); //Welcomes the new user
            }
            catch (Exception)
            {
                await channel.SendMessageAsync($"The welcome message has not been specified. Please type !setwelcomemessage message to set it.");
            }
            IReadOnlyCollection<SocketRole> roles = user.Guild.Roles; //Get the guild roles
            foreach (SocketRole memberRole in roles)
            {
                if (memberRole.Name == "Member") //Assign the member role
                {
                    await user.AddRoleAsync(memberRole);
                    return;
                }
            }
        }
        
        /// <summary>
        /// Called when a message is received in either a channel or DM.
        /// Checks if the message contains a banned word and purges and warns if it does.
        /// If the message is a command, checks if the guild has a bot spam channel and if it does, if the message is posted in there.
        /// Gives one gold per non-command message sent.
        /// </summary>
        /// <param name="MessageParam">Information about the message sent.</param>
        public async Task Client_MessageReceived(SocketMessage MessageParam)
        {
            SocketUserMessage Message = MessageParam as SocketUserMessage;
            SocketCommandContext Context = new SocketCommandContext(Client, Message);
            
            //Make sure the message isn't empty
            if (Context.Message == null || Context.User.IsBot) return;

            //Channel to send the chat logs in
            ulong channel = Data.GetChatLogChannel(Context.Guild.Id);
            if (channel != 0)
            {
                try
                {
                    SocketTextChannel channelPost = Context.Guild.GetTextChannel(channel);
                    
                    //Format: Name#1111: "This is a sentence." -- #channel at 1/11/1111 1:11:11 PM EST
                    timeUtc = DateTime.UtcNow;
                    today = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, easternZone);
                    if (Context.Message.Content == "")
                    {
                        await channelPost.SendMessageAsync($"{Context.Message.Author} uploaded an image. -- <#{Context.Channel.Id}> at {today} EST");
                    }
                    else
                        await channelPost.SendMessageAsync($"{Context.Message.Author}: \"{Context.Message}\" -- <#{Context.Channel.Id}> at {today} EST");
                }
                catch (Exception)
                {
                    
                }
            }

            //If message is blank (picture/file), return after chat log
            if (Context.Message.Content == "")
                return;

            //Get the permissions of the user for use in checking for banned words and checking the bot spam channel
            SocketGuildUser User = Context.User as SocketGuildUser;

            //Make sure the message has no banned words
            //If it contains a banned word, purge the message and issue a warning
            //Three warnings and offender gets kicked
            if (Data.GetBannedWords(Context.Guild.Id).Length > 0 && User.GuildPermissions.KickMembers == false)
            {
                foreach (string word in Data.GetBannedWords(Context.Guild.Id))
                {
                    if (Message.Content.ToLower().Contains(word))
                    {
                        try
                        {   //get the author of the message
                            IGuildUser GuildUser = (IGuildUser)Message.Author;
                            ulong userid = GuildUser.Id;
                            ulong guildid = Context.Guild.Id;
                            string username = GuildUser.Username;

                            //Get the message and delete it, issue a warning
                            await (Context.Channel as SocketTextChannel).DeleteMessageAsync(Context.Message.Id);
                            await Data.AddWarnings(Context.User.Id, Context.Guild.Id, Context.User.Username);
                            int amountOfWarnings = Data.GetWarnings(userid, guildid);

                            //For the mod log to know which moderator to put
                            SocketUser Nephry = Context.Guild.GetUser(322806920203337740);

                            //If warnings is 3 or more, kick the offender if bot has kick permissions
                            if (amountOfWarnings >= 3)
                            {
                                IRole nephry = null;
                                foreach (IRole role in Context.Guild.Roles)
                                {
                                    if (role.Name == "Nephry")
                                    {
                                        nephry = role;
                                        break;
                                    }
                                }

                                if (nephry == null)
                                    return;

                                if (nephry.Permissions.KickMembers == false)
                                    return;

                                await GuildUser.SendMessageAsync("You were kicked for accumulating too many warnings.");
                                await Context.Channel.SendMessageAsync($"{GuildUser.Mention} has been kicked for accumulating too many warnings.");
                                await GuildUser.KickAsync("Accumulated too many warnings");
                                await Data.RemoveWarnings(userid, guildid, username, amountOfWarnings);
                                await ModLog.PostInModLog(Context.Guild, "Auto-Kicked", Nephry, GuildUser, "Accumulated too many warnings.");
                                return;
                            }
                            await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you have been warned for using inappropriate language. Please type \"!getbannedwords\" " +
                                $"to see a list of banned words in this server. " +
                                $"You now have {Data.GetWarnings(Context.User.Id, Context.Guild.Id)} warning(s).");
                            await ModLog.PostInModLog(Context.Guild, "Auto-Warn", Nephry, GuildUser, "Used inappropriate language.");
                            return;
                        }
                        catch (Exception)
                        {

                        }
                    }
                }
            }

            int ArgPos = 0;
            //Make sure the message doesn't start with ! for the gold bonus
            if (!Message.HasCharPrefix('!', ref ArgPos))
                await Data.SaveGold(Context.User.Id, 1, Context.Guild.Id, Context.User.Username); //Add one gold per message

            //If message does not start with ! or bot mention, return
            if (!(Message.HasCharPrefix('!', ref ArgPos) || Message.HasMentionPrefix(Client.CurrentUser, ref ArgPos))) return;
            
            //Get the bot spam channel if there is one assigned
            //and make sure the command is in that channel
            if (Data.GetBotSpamChannel(Context.Guild.Id) != 0)
            {
                //put channel bot spam here and make sure the message is either in the correct channel, has kick or admin permissions
                if (Context.Channel.Id == Data.GetBotSpamChannel(Context.Guild.Id) || User.GuildPermissions.KickMembers == true ||
                    User.GuildPermissions.Administrator == true)
                {
                    IResult Results = await Commands.ExecuteAsync(Context, ArgPos, null);
                    if (!Results.IsSuccess)
                    {
                        CustomCommand customCommands = new CustomCommand();
                        await customCommands.UseCustomCommand(Context);
                    }
                    return;
                }
                else
                    return;
            }
            //Search for a matching command and execute it
            IResult Result = await Commands.ExecuteAsync(Context, ArgPos, null);
            if (!Result.IsSuccess)
            {
                CustomCommand customCommands = new CustomCommand(); //If a hard-coded command isn't found, use the server's custom commands instead
                await customCommands.UseCustomCommand(Context);
            }
        }

        /// <summary>
        /// Called when a message is edited. If there is a chat log channel assigned to the guild, the edited message is posted in it.
        /// </summary>
        /// <param name="word">The old message before the edit.</param>
        /// <param name="MessageParam">The new message after the edit.</param>
        /// <param name="channel">The channel the edit happened in.</param>
        private async Task Client_MessageUpdated(Cacheable<IMessage, ulong> word, SocketMessage MessageParam, ISocketMessageChannel channel)
        {
            SocketGuildChannel guildChannel = MessageParam.Channel as SocketGuildChannel;
            ulong guild = guildChannel.Guild.Id;
            ulong logChannel = Data.GetChatLogChannel(guild);
            if (logChannel != 0)
            {
                IMessage words = await word.GetOrDownloadAsync();
                if (words.Author.IsBot)
                    return;

                SocketTextChannel channelPost = guildChannel.Guild.GetTextChannel(logChannel);

                try
                {
                    //Convert the time to EST
                    timeUtc = DateTime.UtcNow;
                    today = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, easternZone);

                    //Format: Name#1111: "This is a sentence." -- #channel at 1/11/1111 1:11:11 PM EST
                    await channelPost.SendMessageAsync($"{words.Author} has edited \"{words.Content}\" " +
                        $"to \"{MessageParam.Content}\" in <#{channel.Name}> at {today} EST");
                }
                catch(Exception)
                {
                    await channel.SendMessageAsync("I do not have permission to post in the chat log channel.");
                }
            }
        }
    }
}

//Used for creating a .exe to run the bot with.
//dotnet publish -c Debug -r win10-x64