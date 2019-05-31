using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace DiscordBot.Core.Moderation
{
    public class ModerationCommands : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Sets up roles in the server to make the muting system work.
        /// Creates a "Member" and "Muted" role.
        /// Removes the ability to send message from every role except "Member".
        /// Requires administrator permissions to use.
        /// </summary>
        [Command("rolesetup"), Summary("Set up the roles for the mute system to work")]
        public async Task RoleSetup()
        {
            SocketGuildUser checkUser = Context.User as SocketGuildUser;
            if (!checkUser.GuildPermissions.Administrator)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, This command requires the administrator permission.");
                return;
            }
            
            //Get all the users in the guild
            IReadOnlyCollection<SocketGuildUser> users = Context.Guild.Users;
            bool memberFound = false; //If the Member role was found
            bool muteFound = false;   //If the Muted role was found
            IReadOnlyCollection<SocketRole> roles = Context.Guild.Roles; //List of all the server roles
            IRole member = null; //Holds the member role for adding to each user

            //Search all the roles for "Member" and "Muted"
            foreach (SocketRole role in roles)
            {
                if (role.Name == "Member")
                {
                    memberFound = true;
                    member = role;
                }
                else if (role.Name == "Muted")
                {
                    muteFound = true;
                }
                else if (role.Name == "@everyone")
                {
                    await role.ModifyAsync(x => //Change @everyone role so it can only read messages
                    {
                        GuildPermissions noPermissions = new GuildPermissions(false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
                                                false, false, false, false, false, false, false, false, false, false, false, false, false);
                        x.Permissions = noPermissions;
                    });
                }
                else if (role.Name != "Nephry") //Change the send message permission of every other role to false
                {
                    GuildPermissions permissions = role.Permissions; //Permission to change
                    GuildPermissions newPermissions = permissions.Modify(null, null, null, null, null, null, false, null, null, false, null, null, null, null, null, null,
                        null, null, false, null, null, null, null, null, null, null, null, null, null);
                    try
                    {
                        await role.ModifyAsync(x => //Change the permissions of the role
                        {
                            x.Permissions = newPermissions;
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }

            //Channel to send the mod logs in
            await ModLog.PostInModLog(Context.Guild, "Set up roles", Context.User, null, "");

            //If the member role was not found, create it
            if (memberFound == false)
            {
                try
                {
                    GuildPermissions pm = new GuildPermissions(false, false, false, false, false, false, true, false, true, true, false, false, true, true, true, false,
                                                    true, true, true, false, false, false, true, false, true, false, false, false, false);
                    await Context.Guild.CreateRoleAsync("Member", pm);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            //If the mute role was not found, create it
            if (muteFound == false)
            {
                GuildPermissions pm = new GuildPermissions(false, false, false, false, false, false, false, false, true,
                    false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false);

                await Context.Guild.CreateRoleAsync("Muted", pm);
            }

            //If member did not exist before, get it now
            if (member == null)
            {
                foreach (SocketRole role in roles)
                {
                    if (role.Name == "Member")
                    {
                        memberFound = true;
                        member = role;
                        break;
                    }
                }
            }

            //Give every user the "Member" role
            foreach (SocketGuildUser user in users)
            {
                IGuildUser use = user;
                await use.AddRoleAsync(member);
            }

            await Context.Channel.SendMessageAsync($"{Context.User.Mention}, Role setup successfully complete.");
        }

        /// <summary>
        /// Kicks the target from the guild and sends them the reason why.
        /// Requires kick permissions to use.
        /// </summary>
        /// <param name="user">The user to kick.</param>
        /// <param name="reason">Why they were kicked.</param>
        [Command("kick"), Alias("Kick"), Summary("Kick the mention'd user")]
        public async Task KickUser(IGuildUser user = null, [Remainder]string reason = "No reason provided")
        {
            //Make sure user issuing command has perms
            SocketGuildUser checkUser = Context.User as SocketGuildUser;
            if (!checkUser.GuildPermissions.KickMembers && !checkUser.GuildPermissions.Administrator)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, You do not have kick permissions on this server.");
                return;
            }

            //Make sure a user is targeted
            if (user == null)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Username}, you must specify who you want to kick!");
            }

            if (user.Id == 322806920203337740)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, I cannot kick myself!");
                return;
            }

            //Make sure the bot has kick permissions
            SocketGuildUser Nephry = Context.Guild.GetUser(322806920203337740);
            if (!Nephry.GuildPermissions.KickMembers)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention} I do not have perms to kick users!");
                return;
            }

            try
            {
                //Send the target a DM saying why they were kicked
                await user.SendMessageAsync($"You have been kicked by {Context.User.Mention} for \"{reason}\".");

                //Display the target was kicked
                await Context.Channel.SendMessageAsync($"{user.Mention} has been kicked by {Context.User.Mention} for \"{reason}\"");
                await user.KickAsync(reason); //Kick the target

                //Channel to send the mod logs in
                await ModLog.PostInModLog(Context.Guild, "Kick", Context.User, user, reason);
            }
            catch (Exception)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Username}, I do not have permission " +
                    $"to kick on this server or my role is too low on the role list.");
            }
        }

        /// <summary>
        /// Bans the target from the server and sends them why.
        /// Requires kick permissions to use.
        /// </summary>
        /// <param name="user">The user to ban.</param>
        /// <param name="reason">The reason they were banned.</param>
        [Command("ban"), Alias("Ban"), Summary("Ban the mention'd user")]
        public async Task BanUser(IGuildUser user = null, [Remainder]string reason = "No reason provided")
        {
            SocketGuildUser checkUser = Context.User as SocketGuildUser;
            if (!checkUser.GuildPermissions.BanMembers && !checkUser.GuildPermissions.Administrator) //Make sure user issuing command is admin
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, You do not have ban permissions on this server.");
                return;
            }

            //Make sure someone is mention'd
            if (user == null)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you must specify who you want to ban!");
                return;
            }

            if (user.Id == 322806920203337740)
            {
                await Context.Channel.SendMessageAsync("I cannot ban myself!");
                return;
            }

            //Make sure the bot has ban permissions
            SocketGuildUser Nephry = Context.Guild.GetUser(322806920203337740);
            if (!Nephry.GuildPermissions.BanMembers)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention} I do not have perms to ban users!");
                return;
            }

            try
            {   //Ban the user and say they were banned
                await user.SendMessageAsync($"You have been banned by {Context.User.Mention} for \"{reason}\".");
                await Context.Channel.SendMessageAsync($"{user.Mention} has been banned by {Context.User.Mention}");
                await user.Guild.AddBanAsync(user, 5, reason);

                //Channel to send the mod logs in
                await ModLog.PostInModLog(Context.Guild, "Ban", Context.User, user, reason);
            }
            catch (Exception)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, I do not have permission to ban on this server.");
            }
        }

        /// <summary>
        /// Deletes X amount of message in chat where X is the amount given when using the command.
        /// Requires kick permissions to use.
        /// </summary>
        /// <param name="amount">The amount of messages to delete.</param>
        /// <param name="reason">Why the channel has been purged.</param>
        [Command("purge"), Alias("erase", "exterminatus"), Summary("Delete the last N messages from the channel")]
        public async Task Purge(int amount = 10, [Remainder]string reason = "No reason given")
        {
            SocketGuildUser checkUser = Context.User as SocketGuildUser;
            if (!checkUser.GuildPermissions.KickMembers && !checkUser.GuildPermissions.Administrator) //Make sure user issuing command is admin
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you cannot purge messages on this server, this requires the kick permission.");
                return;
            }

            //Make sure the amount is less than 51
            if (amount > 50)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, purging amount is limited to 50 due to rate limitations.");
                return;
            }

            if (amount < 1)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, please make sure you are purging at least 1 message.");
                return;
            }

            //Include the purge command in the count
            amount += 1;

            try
            {
                IEnumerable<IMessage> allMessages = await this.Context.Channel.GetMessagesAsync(amount).FlattenAsync();
                await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(allMessages);
                Discord.Rest.RestUserMessage message = await Context.Channel.SendMessageAsync($"{Context.User.Mention}, {amount - 1} messages have been purged. Deleting this message in five seconds.");
                System.Timers.Timer timer = new Timer(5000);
                // Hook up the Elapsed event for the timer. 
                timer.Elapsed += (s, e) => (Context.Channel as SocketTextChannel).DeleteMessageAsync(message);
                timer.AutoReset = false;
                timer.Enabled = true;

                //Channel to send the mod logs in
                await ModLog.PostInModLog(Context.Guild, "Purge", Context.User, null, reason);

            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, I do not have the \"Manage Messages\" permission.");
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Deletes messages by a specified user.
        /// Retrieves the last 50 messages in the channel and check who sent each one.
        /// If the target'd user sent the message it is deleted.
        /// Requires kick permissions to use.
        /// </summary>
        /// <param name="user">The user whose messages are being deleted.</param>
        /// <param name="reason">Why the user is being purged.</param>
        [Command("purge"), Alias("erase", "exterminatus"), Summary("Delete the recent messages from the mention'd user")]
        public async Task PurgeUser(IGuildUser user = null, [Remainder]string reason = "No reason given")
        {
            try
            {
                //Get the last 50 messages in the channel
                IEnumerable<IMessage> specificMessages = await this.Context.Channel.GetMessagesAsync(50).FlattenAsync();

                //The list of messages to delete
                List<IMessage> messagesToDelete = new List<IMessage>();

                //Add the message to the list if it's by the user to delete
                foreach (IMessage messages in specificMessages)
                {
                    if (messages.Author.Id == user.Id)
                    {
                        messagesToDelete.Add(messages);
                    }
                }

                //Convert the list into an IEnumerable as required by the DeleteMessagesAsync method
                IEnumerable<IMessage> enumerableMessages = messagesToDelete;

                //Delete the messages by the user
                await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(enumerableMessages);

                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, I have deleted all recent messages by {user.Mention} because of \"{reason}\"");

                //Channel to send the mod logs in
                await ModLog.PostInModLog(Context.Guild, "Purge User", Context.User, user, reason);
                return;
            }
            catch (Exception)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, I do not have the \"Manage Messages\" permission.");
            }
        }

        /// <summary>
        /// Gives the target a warning. If the bot has kick permissions, the target is auto-kicked after 3 warnings.
        /// Requires kick permissions to use.
        /// </summary>
        /// <param name="user">The user being warned.</param>
        /// <param name="reason">The reason for the warning.</param>
        [Command("warn"), Summary("Warn a user")]
        public async Task WarnUser(IGuildUser user = null, [Remainder]string reason = "No reason specified")
        {
            SocketGuildUser checkUser = Context.User as SocketGuildUser;
            if (!checkUser.GuildPermissions.KickMembers && !checkUser.GuildPermissions.Administrator) //Make sure user issuing command is admin
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, You cannot warn users, this requires the kick permission.");
                return;
            }
            //Make sure someone is targeted
            if (user == null)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you must specify who you want to warn!");
                return;
            }
            //Don't warn bots
            if (user.IsBot)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you cannot warn a bot!");
                return;
            }

            ulong userid = user.Id; //Holds user information
            ulong guildid = Context.Guild.Id;
            string username = user.Username;
            
            await Data.Data.AddWarnings(userid, guildid, username); //Add a warning
            int amountOfWarnings = Data.Data.GetWarnings(userid, guildid);

            //If user has 3 or more warnings, auto-kick
            if (amountOfWarnings >= 3)
            {
                SocketGuildUser nephry; //Get nephry
                nephry = Context.Guild.GetUser(322806920203337740);

                if (nephry == null)
                    return;

                //Make sure nephry has kick permissions
                if (nephry.GuildPermissions.KickMembers == false)
                {
                    amountOfWarnings = Data.Data.GetWarnings(user.Id, Context.Guild.Id);
                    await Context.Channel.SendMessageAsync($"{user.Mention}, you have been warned for \"{reason}\". You now have {amountOfWarnings} warning(s).");

                    //Channel to send the mod logs in
                    string actions;
                    switch (amountOfWarnings)
                    {
                        case 1:
                            actions = $"Warn ({amountOfWarnings}st Warning.)";
                            break;
                        case 2:
                            actions = $"Warn ({amountOfWarnings}nd Warning)";
                            break;
                        case 3:
                            actions = $"Warn ({amountOfWarnings}rd Warning)";
                            break;
                        default:
                            actions = $"Warn ({amountOfWarnings}th Warning)";
                            break;
                    }
                    await ModLog.PostInModLog(Context.Guild, actions, Context.User, user, reason);
                    return;
                }
                    

                //Kick the user
                await user.SendMessageAsync("You were kicked for accumulating too many warnings.");
                await Context.Channel.SendMessageAsync($"{user.Mention} has been kicked for accumulating too many warnings.");
                await user.KickAsync("Accumulated too many warnings");
                await Data.Data.RemoveWarnings(userid, guildid, username, amountOfWarnings);

                //Channel to send the mod logs in
                await ModLog.PostInModLog(Context.Guild, "Warn", Context.User, user, reason);
                await ModLog.PostInModLog(Context.Guild, "Auto-Kicked for too many warnings.", nephry, user, reason);
                return;
            }
            //Display how many warnings the user has
            int currentAmount = Data.Data.GetWarnings(user.Id, Context.Guild.Id);
            await Context.Channel.SendMessageAsync($"{user.Mention}, you have been warned for \"{reason}\". You now have {currentAmount} warning(s).");

            //Channel to send the mod logs in
            string action = "Warn";
            switch (currentAmount)
            {
                case 1:
                    action = $"Warn ({currentAmount}st Warning.)";
                    break;
                case 2:
                    action = $"Warn ({currentAmount}nd Warning)";
                    break;
                case 3:
                    action = $"Warn ({currentAmount}rd Warning)";
                    break;
                default: action = $"Warn ({currentAmount}th Warning)";
                    break;
            }
            await ModLog.PostInModLog(Context.Guild, action, Context.User, user, reason);
        }

        /// <summary>
        /// Removes N warnings from the user where N is the amount given when using the command.
        /// Requires kick permissions to use.
        /// </summary>
        /// <param name="user">The target to remove warnings from.</param>
        /// <param name="amount">The amount of warnings to remove, defaulted at 1.</param>
        /// <param name="reason">Why the warnings were removed.</param>
        [Command("removewarn"), Alias("removewarning", "removewarnings"), Summary("Remove N warnings from the user")]
        public async Task RemoveWarn(IGuildUser user = null, int amount = 1, [Remainder]string reason = "No reason given")
        {
            SocketGuildUser checkUser = Context.User as SocketGuildUser;
            if (!checkUser.GuildPermissions.KickMembers && !checkUser.GuildPermissions.Administrator) //Make sure user issuing command is admin
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, You cannot remove warnings, this requires the kick permission.");
                return;
            }

            //Make sure someone is targeted
            if (user == null)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you must specify who you want to remove a warning from!");
            }
            
            //Don't target bots
            if (user.IsBot)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you cannot remove a warning from a bot!");
                return;
            }

            //Remove the warnings
            await Data.Data.RemoveWarnings(user.Id, Context.Guild.Id, user.Username, amount);
            await Context.Channel.SendMessageAsync($"{user.Mention}, you have had {amount} warning(s) removed for \"{reason}\". " +
                $"You now have {Data.Data.GetWarnings(user.Id, Context.Guild.Id)} warning(s).");

            //Channel to send the mod logs in
            await ModLog.PostInModLog(Context.Guild, "Remove warning", Context.User, user, reason);
        }

        /// <summary>
        /// Mutes the target by taking their Member role away and giving the muted role.
        /// Only works if the "rolesetup" command has been used correctly.
        /// Requires kick permissions to use.
        /// </summary>
        /// <param name="user">The user to mute.</param>
        /// <param name="amount">The length of time to mute for, 0 is a permanent mute. Default value is 0.</param>
        /// <param name="reason">The reason for the mute.</param>
        [Command("mute"), Summary("Revokes the 'Member' role so the user cannot talk in chat, adds 'Muted' role to show they are muted")]
        public async Task AddMute(SocketGuildUser user = null, int amount = 0, [Remainder]string reason = "No reason given")
        {
            //Test if the user using the command has the kick or administrator permission
            SocketGuildUser checkUser = Context.User as SocketGuildUser;
            if (!checkUser.GuildPermissions.KickMembers && checkUser.GuildPermissions.Administrator) //Make sure user issuing command is admin
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, You cannot mute users, this requires the kick permission.");
                return;
            }

            //Make sure a target was specified
            if (user == null)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you must specify someone to mute!");
                return;
            }

            if (user.Id == 322806920203337740)
            {
                await Context.Channel.SendMessageAsync("I cannot mute myself!");
                return;
            }

            //Holds if the role exists
            bool muteFound = false;
            IReadOnlyCollection<SocketRole> roles = Context.Guild.Roles;
            foreach (SocketRole mutedRole in roles) //Find the role
            {
                if (mutedRole.Name == "Muted")
                {
                    muteFound = true;//Role was found
                    await (user).AddRoleAsync(mutedRole); //Add the muted role
                    bool memberFound = false; //Holds if the member role was found
                    if (amount == 0) //If there was no time specified, make mute permanent
                    {
                        foreach (SocketRole memberRole in roles)
                        {
                            if (memberRole.Name == "Member")
                            {
                                //Remove the member role
                                await user.RemoveRoleAsync(memberRole);
                                //Say target was muted
                                await Context.Channel.SendMessageAsync($"{user.Mention}, you have been permanently muted by {Context.User.Mention} for \"{reason}\".");

                                //Channel to send the mod logs in
                                await ModLog.PostInModLog(Context.Guild, "Mute", Context.User, user, reason);
                                return;
                            }
                        }

                    }
                    else //Time limit on mute was placed
                    {
                        foreach (SocketRole roleMember in roles)
                        {
                            if (roleMember.Name == "Member")
                            {   //Remove the member role
                                await user.RemoveRoleAsync(roleMember);
                                memberFound = true;
                                break;
                            }
                        }
                        if (memberFound == true)
                        {
                            try
                            {   //Say user was muted for how long
                                await Context.Channel.SendMessageAsync($"{user.Mention}, you have been muted by {Context.User.Mention} for {amount} seconds for \"{reason}\".");
                                Timer timer = new Timer(amount * 1000);
                                // Hook up the Elapsed event for the timer. 
                                timer.Elapsed += async (s, e) => await RemoveTempMute(user);
                                timer.AutoReset = false;
                                timer.Enabled = true;

                                //Channel to send the mod logs in
                                await ModLog.PostInModLog(Context.Guild, "Mute", Context.User, user, reason);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
                        }
                    }

                    //If the member role was not found, tell the user to type '!rolesetup'
                    if (memberFound == false)
                    {
                        await Context.Channel.SendMessageAsync("The Member role has not been found. Make sure to put the bot at the top of the role list" +
                            " then type !rolesetup in order for the muting system to work.");
                    }

                    return;
                }
            }

            //If the mute role was not found, tell the user to type '!rolesetup
            if (muteFound == false)
            {
                await Context.Channel.SendMessageAsync("The Muted role has not been found. Make sure to put the bot at the top of the role list" +
                            " then type !rolesetup in order for the muting system to work.");
            }
        }

        /// <summary>
        /// Unmutes the target user. Gives the member role back and removes the muted role. Requires kick permissions to use.
        /// </summary>
        /// <param name="target">The target to unmute.</param>
        /// <param name="reason">The reason for the unmute.</param>
        /// <returns></returns>
        [Command("unmute"), Summary("Gives back the 'Member' role so the user can talk in chat, removes 'Muted' role")]
        public async Task RemoveMute(SocketGuildUser target = null, [Remainder]string reason = "No reason given")
        {
            SocketGuildUser checkUser = Context.User as SocketGuildUser;
            if (!checkUser.GuildPermissions.KickMembers && !checkUser.GuildPermissions.Administrator) //Make sure user issuing command is admin
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, You cannot unmute users, this requires the kick permission.");
                return;
            }

            if (target == null)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you must specify someone to unmute!");
                return;
            }
            IReadOnlyCollection<SocketRole> roles = Context.Guild.Roles;

            //Holds if the roles were found
            bool memberFound = false;
            bool mutedFound = false;

            foreach (SocketRole memberRole in roles)
            {
                if (memberRole.Name == "Member")
                {
                    memberFound = true;
                    await (target).AddRoleAsync(memberRole);

                    foreach (SocketRole mutedRole in roles)
                    {
                        if (mutedRole.Name == "Muted")
                        {
                            mutedFound = true;

                            //Make sure the user is still muted, if not then return
                            IReadOnlyCollection<SocketRole> userToCheck = target.Roles;

                            foreach (SocketRole role in userToCheck)
                            {
                                if (role.Name == "Muted")
                                {
                                    await (target as IGuildUser).RemoveRoleAsync(role);
                                    await Context.Channel.SendMessageAsync($"{target.Mention}, you have been unmuted by {Context.User.Mention}.");

                                    //Channel to send the mod logs in
                                    await ModLog.PostInModLog(Context.Guild, "Unmute", Context.User, target, reason);
                                    return;
                                }
                            }
                            await Context.Channel.SendMessageAsync($"{target.Mention} is not currently muted!");
                        }
                    }
                }
            }
            //If the muting system is not set up
            if (!memberFound || !mutedFound)
            {
                await Context.Channel.SendMessageAsync("Either the member, muted, or both role(s) has not been found. Make sure to put the bot at the top of the role list" +
                            " then type !rolesetup in order for the muting system to work.");
            }
        }

        /// <summary>
        /// Unmutes the user when the timed mute expires. Adds the member role and removes the muted role.
        /// </summary>
        /// <param name="user">The user to unmute.</param>
        private async Task RemoveTempMute(SocketGuildUser user)
        {
            IReadOnlyCollection<SocketRole> roles = Context.Guild.Roles;

            //Holds if the roles were found
            bool memberFound = false;
            bool mutedFound = false;

            foreach (SocketRole member in roles)
            {
                if (member.Name == "Member")
                {
                    memberFound = true;
                    await (user).AddRoleAsync(member);

                    foreach (SocketRole muted in roles)
                    {
                        if (muted.Name == "Muted")
                        {
                            mutedFound = true;

                            //Make sure the user is still muted, if not then return
                            IReadOnlyCollection<SocketRole> userToCheck = user.Roles;

                            foreach (SocketRole role in userToCheck)
                            {
                                if (role.Name == "Muted")
                                {
                                    await user.RemoveRoleAsync(muted);
                                    await Context.Channel.SendMessageAsync($"{user.Mention}, your timed mute has expired. You can now chat again!");

                                    SocketUser Nephry = Context.Guild.GetUser(322806920203337740);
                                    //Channel to send the mod logs in
                                    await ModLog.PostInModLog(Context.Guild, "Auto-Unmuted", Nephry, user, "Mute time expired.");

                                    return;
                                }
                            }
                        }
                    }
                }
            }
            //If the muting system has not been setup
            if (!memberFound || !mutedFound)
            {
                await Context.Channel.SendMessageAsync("Either the member, muted, or both role(s) has not been found. Make sure to put the bot at the top of the role list" +
                            " then type !rolesetup in order for the muting system to work.");
            }
        }

        /// <summary>
        /// Adds a banned word/phrase to the banned word list for this guild. Requires administrator permissions.
        /// </summary>
        /// <param name="word">The word/phrase to add to the ban list.</param>
        [Command("addbannedword"), Alias("addbanword", "banword"), Summary("Add a word to the banned word list")]
        public async Task AddBannedWord([Remainder] string word)
        {
            //Test if the user using the command has the kick or administrator permission
            SocketGuildUser checkUser = Context.User as SocketGuildUser;
            if (!checkUser.GuildPermissions.Administrator) //Make sure user issuing command is admin
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, You cannot ban words, this requires you to be an administrator.");
                return;
            }
            //Add the word
            await Data.Data.AddBannedWord(Context.Guild.Id, word.ToLower());
            await Context.Channel.SendMessageAsync($"{Context.User.Mention}, \"{word}\" was successfully added to the word ban list.");
            
            //Channel to send the mod logs in
            await ModLog.PostInModLog(Context.Guild, "Added banned word: " + word, Context.User, null, "");
        }

        /// <summary>
        /// Removes the banned word/phrase from the list. Requires administrator permissions.
        /// </summary>
        /// <param name="word">The word/phrase to remove.</param>
        [Command("removebannedword"), Alias("removebanword", "removeword"), Summary("Remove a word from the banned word list")]
        public async Task RemoveBannedWord([Remainder] string word)
        {
            //Test if the user using the command has the kick or administrator permission
            SocketGuildUser checkUser = Context.User as SocketGuildUser;
            if (!checkUser.GuildPermissions.Administrator) //Make sure user issuing command is admin
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, You cannot remove banned words, this requires you to be an administrator.");
                return;
            }

            bool result;
            result = await Data.Data.RemoveBannedWord(Context.Guild.Id, word.ToLower());

            if (result)
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, \"{word}\" was successfully removed from the word ban list.");
            else
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, \"{word}\" was not found in the ban list. Are you sure it was spelled correctly?");

            //Channel to send the mod logs in
            await ModLog.PostInModLog(Context.Guild, "Removed banned word: " + word, Context.User, null, "");

        }

        /// <summary>
        /// DMs the user a list of words/phrases that are currently banned in the guild.
        /// </summary>
        [Command("bannedwords"), Alias("bannedword", "bannedwordlist", "bannedwordslist"), Summary("Gets the banned words in the guild")]
        public async Task GetBannedWords()
        {
            if (Data.Data.GetBannedWords(Context.Guild.Id).Length > 0)
            {
                string[] wordList = Data.Data.GetBannedWords(Context.Guild.Id);
                string words = "Banned Words: ";
                foreach (string word in wordList)
                {
                    words += word;
                    words += ", ";
                }

                //Get rid of the trailing whitespace and comma
                words = words.Trim();
                int length = words.Length;
                words = words.Remove(length - 1);

                //Embedbuilder object
                EmbedBuilder Embed = new EmbedBuilder();

                //Assign the author
                Embed.WithAuthor("Banned Words", Context.User.GetAvatarUrl());

                //Assign the color on the left side
                Embed.WithColor(40, 200, 150);

                //Create the description
                Embed.WithDescription(words);
                await Context.User.SendMessageAsync("", false, Embed.Build());
            }
            else
                await Context.User.SendMessageAsync("There are no banned words in this server.");
        }

        /// <summary>
        /// Sets the nickname of a target'd user. Requires kick permissions to use.
        /// </summary>
        /// <param name="user">The user to change the name of.</param>
        /// <param name="name">The name to change it to.</param>
        /// <param name="reason">The reason for the name change.</param>
        /// <returns></returns>
        [Command("setnickname"), Summary("Set the nickname of a user")]
        public async Task SetNickname(SocketGuildUser user, string name, [Remainder]string reason = "No reason given.")
        {
            SocketGuildUser checkUser = Context.User as SocketGuildUser;
            if (!checkUser.GuildPermissions.KickMembers && !checkUser.GuildPermissions.Administrator) //Make sure user issuing command is admin
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, this command requires the kick permission! " +
                    $"Are you looking for the store command \"!nicknametarget?\"");
                return;
            }

            try
            {
                string oldName = user.Nickname;
                await user.ModifyAsync(x => x.Nickname = name);
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, the nickname for {user.Mention} has been successfully changed.");
                await ModLog.PostInModLog(Context.Guild, $"Changed Nickname - Was \"{oldName}\"", Context.User, user, reason);
            }
            catch (Exception)
            {
                await Context.Channel.SendMessageAsync("My role is not above the target user's highest role. Please move me higher :grinning:");
            }
        }
    }
}