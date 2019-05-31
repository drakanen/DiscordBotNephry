using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DiscordBot.Resources.Database;
using System.Linq;

namespace DiscordBot.Core.Data
{
    public static class Data
    {
        //Everything in here uses Sqlite to access the database
        /// <summary>
        /// Set the interest rate on giving gold in the server in whole number form.
        /// Converts to a percentage. Defaults at 5.
        /// </summary>
        /// <param name="serverId">The guild id</param>
        /// <param name="serverName">The guild name</param>
        /// <param name="percentage">The number to change the percentage to. 5 would be 0.05%</param>
        public static async Task SetInterest(ulong serverId, string serverName, int percentage)
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                try
                {   //Check if the guild already has a spot
                    if (DbContext.GuildLocationSettings.Where(x => x.Serverid == serverId).Count() < 1)
                    {
                        DbContext.GuildLocationSettings.Add(new GuildLocationSettings
                        {   //Default the other settings.
                            Serverid = serverId,
                            WelcomeChannel = 0,
                            ServerName = serverName,
                            WelcomeMessage = "",
                            BotSpamChannel = 0,
                            GoldInterest = percentage
                        });
                    }
                    else
                    {   //Override the percentage value
                        GuildLocationSettings Current = DbContext.GuildLocationSettings.Where(x => x.Serverid == serverId).FirstOrDefault();
                        Current.GoldInterest = percentage;
                        DbContext.GuildLocationSettings.Update(Current);
                    } //Update the database
                    await DbContext.SaveChangesAsync();
                }
                catch (Exception)
                { }
            }
        }

        /// <summary>
        /// Returns the interest rate as a whole number. 0.05% would return as 5.
        /// </summary>
        /// <param name="serverId">The guild ID to get the interst rate of.</param>
        public static int GetInterest(ulong serverId)
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                try
                {   //Check if the guild has a spot
                    if (DbContext.Gold.Where(x => x.Serverid == serverId).Count() < 1)
                    {
                        return 0;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                } //Return the interest rate in the guild
                return DbContext.GuildLocationSettings.Where(x => x.Serverid == serverId).Select(x => x.GoldInterest).FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets the amount of gold the user has
        /// </summary>
        /// <param name="userId">The user ID to look for</param>
        /// <param name="serverId">The guild ID to look for</param>
        /// <returns>The amount of gold the user has</returns>
        public static int GetGold(ulong userId, ulong serverId)
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                try
                {   //Check if the guild already has a spot
                    if (DbContext.Gold.Where(x => x.UserId == userId && x.Serverid == serverId).Count() < 1)
                    {
                        return 0;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }   //Return the amount of gold the user has in the guild
                return DbContext.Gold.Where(x => x.UserId == userId && x.Serverid == serverId).Select(x => x.Amount).FirstOrDefault();
            }
        }

        /// <summary>
        /// Returns the top ten users who have the most gold in the guild
        /// </summary>
        /// <param name="serverId">The guild to look for</param>
        /// <returns>A gold array with the user's name and amount of gold they have in each element.</returns>
        public static Gold[] GetTopTenGold(ulong serverId)
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                try
                {   //Check if the guild has a spot
                    if (DbContext.Gold.Where(x => x.Serverid == serverId).Count() < 1)
                    {
                        return null;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                //Return the name of the user and amount of gold they have for the top 10 amount of gold
                IQueryable<Gold> topTen = (from gold in DbContext.Gold
                              where gold.Serverid == serverId
                              orderby gold.Amount descending
                              select gold).Take(10);
                return topTen.ToArray();
            }
        }

        /// <summary>
        /// Adds gold to the amount the user currently has.
        /// </summary>
        /// <param name="userId">The user ID to look for</param>
        /// <param name="amount">The amount to add</param>
        /// <param name="serverId">The guild ID to look for</param>
        /// <param name="username">The user's username</param>
        public static async Task SaveGold(ulong userId, int amount, ulong serverId, string username = "")
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                //Check if the guild has a spot
                if (DbContext.Gold.Where(x => x.UserId == userId && x.Serverid == serverId).Count() < 1)
                {
                    try
                    {
                        DbContext.Gold.Add(new Gold
                        {   //Create an entry for this user in this guild if it doesn't exist
                            Serverid = serverId,
                            UserId = userId,
                            Amount = amount,
                            Username = username
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                else
                {   //Add the amount of gold to the current and update the database
                    Gold Current = DbContext.Gold.Where(x => x.UserId == userId && x.Serverid == serverId).FirstOrDefault();
                    Current.Amount += amount;
                    DbContext.Gold.Update(Current);
                }
                await DbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Saves the gold of a large amount of people at once.
        /// </summary>
        /// <param name="userId">An array of user IDs to look for</param>
        /// <param name="amount">The amount to add</param>
        /// <param name="serverId">The guild ID</param>
        public static async Task SaveGoldMass(ulong[] userId, int amount, ulong serverId)
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                foreach (ulong user in userId)
                {
                    Gold Current = DbContext.Gold.Where(x => x.UserId == user && x.Serverid == serverId).FirstOrDefault();
                    Current.Amount += amount;
                    DbContext.Gold.Update(Current);
                }
                await DbContext.SaveChangesAsync();
            }
    }

        /// <summary>
        /// Removes gold from the amount the user currently has
        /// </summary>
        /// <param name="userId">The user ID to look for</param>
        /// <param name="amount">The amount to take</param>
        /// <param name="serverId">The guild ID to look for</param>
        /// <param name="username">The user's username</param>
        public static async Task SaveGoldMinus(ulong userId, int amount, ulong serverId, string username = "")
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                //Check if the guild has a spot
                if (DbContext.Gold.Where(x => x.UserId == userId && x.Serverid == serverId).Count() < 1)
                {
                    DbContext.Gold.Add(new Gold
                    {
                        Serverid = serverId,
                        UserId = userId,
                        Amount = 0 - amount,
                        Username = username
                    });
                }
                else
                {
                    Gold Current = DbContext.Gold.Where(x => x.UserId == userId && x.Serverid == serverId).FirstOrDefault();
                    Current.Amount -= amount;
                    DbContext.Gold.Update(Current);
                }
                await DbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Add a warning to the user's amountr
        /// </summary>
        /// <param name="userId">The user ID to look for</param>
        /// <param name="serverId">The guild ID to look for</param>
        /// <param name="username">The username of the user</param>
        public static async Task AddWarnings(ulong userId, ulong serverId, string username)
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                //Check if the guild has a spot
                if (DbContext.Warnings.Where(x => x.UserId == userId && x.Serverid == serverId).Count() < 1)
                {
                    DbContext.Warnings.Add(new Warning
                    {
                        Serverid = serverId,
                        UserId = userId,
                        AmountOfWarnings = 1,
                        Username = username
                    });
                }
                else
                {
                    Warning Current = DbContext.Warnings.Where(x => x.UserId == userId && x.Serverid == serverId).FirstOrDefault();
                    Current.AmountOfWarnings += 1;
                    DbContext.Warnings.Update(Current);
                }
                await DbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Remove warnings from the user's amountr
        /// </summary>
        /// <param name="userId">The user ID to look for</param>
        /// <param name="serverId">The guild ID to look for</param>
        /// <param name="username">The username of the user</param>
        /// <param name="amount">The amount of warnings to remove, defaulted at 1</param>
        /// <returns></returns>
        public static async Task RemoveWarnings(ulong userId, ulong serverId, string username, int amount = 1)
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                //Check if the guild has a spot
                if (DbContext.Warnings.Where(x => x.UserId == userId && x.Serverid == serverId).Count() > 0)
                {
                    //Check if the spot has an amount
                    if (DbContext.Warnings.Where(x => x.UserId == userId && x.Serverid == serverId).Select(x => x.AmountOfWarnings).FirstOrDefault() > 0)
                    {
                        Warning Current = DbContext.Warnings.Where(x => x.UserId == userId && x.Serverid == serverId).FirstOrDefault();
                        Current.AmountOfWarnings -= amount;
                        if (Current.AmountOfWarnings < 0)
                            Current.AmountOfWarnings = 0;
                        DbContext.Warnings.Update(Current);
                    }
                    else
                        return;
                }
                else
                    return;

                await DbContext.SaveChangesAsync();
            }
        }
        
        /// <summary>
        /// Get the amount of warnings the user has in the guild
        /// </summary>
        /// <param name="userId">The user ID to look for</param>
        /// <param name="serverId">The guild ID to look for</param>
        /// <returns>The amount of warnings the user has in the guild</returns>
        public static int GetWarnings(ulong userId, ulong serverId)
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                try
                {
                    //Check if the guild has a spot
                    if (DbContext.Warnings.Where(x => x.UserId == userId && x.Serverid == serverId).Count() < 1)
                    {
                        return 0;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                return DbContext.Warnings.Where(x => x.UserId == userId && x.Serverid == serverId).Select(x => x.AmountOfWarnings).FirstOrDefault();
            }
        }

        /// <summary>
        /// Sets the welcome channel to display the welcome message in the guild
        /// </summary>
        /// <param name="serverId">The guild ID to look for</param>
        /// <param name="channelId">The channel ID to set as the welcome channel</param>
        /// <param name="serverName">The name of the guild</param>
        public async static Task SetWelcomeChannel(ulong serverId, ulong channelId, string serverName)
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                try
                {
                    //Check if the guild has a spot
                    if (DbContext.GuildLocationSettings.Where(x => x.Serverid == serverId).Count() < 1)
                    {
                        DbContext.GuildLocationSettings.Add(new GuildLocationSettings
                        {
                            Serverid = serverId,
                            WelcomeChannel = channelId,
                            ServerName = serverName,
                            WelcomeMessage = "",
                            BotSpamChannel = 0,
                            GoldInterest = 5,
                            ChatLogChannel = 0
                        });
                    }
                    else
                    {
                        GuildLocationSettings Current = DbContext.GuildLocationSettings.Where(x => x.Serverid == serverId).FirstOrDefault();
                        Current.WelcomeChannel = channelId;
                        Current.ServerName = serverName;
                        DbContext.GuildLocationSettings.Update(Current);
                    }
                    await DbContext.SaveChangesAsync();
                }
                catch (Exception)
                { }
            }
        }

        /// <summary>
        /// Gets the channel to display the welcome message in
        /// </summary>
        /// <param name="serverId">The ID of the guild to look for</param>
        /// <returns>The ulong ID of the channel to post the message in</returns>
        public static ulong GetWelcomeChannel(ulong serverId)
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                return DbContext.GuildLocationSettings.Where(x => x.Serverid == serverId).Select(x => x.WelcomeChannel).FirstOrDefault();
            }
        }

        /// <summary>
        /// Sets the welcome message to display in the welcome channel when a user joins.
        /// </summary>
        /// <param name="serverId">The guild ID to look for</param>
        /// <param name="welcomeMessage">The message to set as the welcome message</param>
        /// <param name="serverName">The name of the guild.</param>
        public async static Task SetWelcomeMessage(ulong serverId, string welcomeMessage, string serverName = "")
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                //Check if the guild has a spot
                if (DbContext.GuildLocationSettings.Where(x => x.Serverid == serverId).Count() < 1)
                {
                    DbContext.GuildLocationSettings.Add(new GuildLocationSettings
                    {
                        Serverid = serverId,
                        WelcomeChannel = 0,
                        ServerName = serverName,
                        WelcomeMessage = welcomeMessage,
                        BotSpamChannel = 0,
                        GoldInterest = 5,
                        ChatLogChannel = 0
                    });
                }
                else
                {
                    GuildLocationSettings Current = DbContext.GuildLocationSettings.Where(x => x.Serverid == serverId).FirstOrDefault();
                    Current.WelcomeMessage = welcomeMessage;
                    DbContext.GuildLocationSettings.Update(Current);
                    await DbContext.SaveChangesAsync();
                }
            }
        }

        /// <summary>
        /// Gets the welcome message to display in the welcome channel
        /// </summary>
        /// <param name="serverid">The guild ID to look for</param>
        /// <returns>The message to display</returns>
        public static string GetWelcomeMessage(ulong serverid)
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                return DbContext.GuildLocationSettings.Where(x => x.Serverid == serverid).Select(x => x.WelcomeMessage).FirstOrDefault();
            }
        }

        /// <summary>
        /// Sets a channel for the bot to only respond in. The bot can respond anywhere if this is 0.
        /// </summary>
        /// <param name="serverId">The guild ID to look for</param>
        /// <param name="channelId">The channel to set as the bot spam channel.</param>
        /// <param name="serverName">The guild name.</param>
        public async static Task SetBotSpamChannel(ulong serverId, ulong channelId, string serverName)
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                try
                {
                    //Check if the guild has a spot
                    if (DbContext.GuildLocationSettings.Where(x => x.Serverid == serverId).Count() < 1)
                    {
                        DbContext.GuildLocationSettings.Add(new GuildLocationSettings
                        {
                            Serverid = serverId,
                            WelcomeChannel = 0,
                            ServerName = serverName,
                            WelcomeMessage = "",
                            BotSpamChannel = channelId,
                            GoldInterest = 5,
                            ChatLogChannel = 0
                        });
                    }
                    else
                    {
                        GuildLocationSettings Current = DbContext.GuildLocationSettings.Where(x => x.Serverid == serverId).FirstOrDefault();
                        Current.BotSpamChannel = channelId;
                        Current.ServerName = serverName;
                        DbContext.GuildLocationSettings.Update(Current);
                    }
                    await DbContext.SaveChangesAsync();
                }
                catch (Exception)
                { }
            }
        }

        /// <summary>
        /// Get the channel the bot has to respond in.
        /// </summary>
        /// <param name="serverId">The guild ID to look for.</param>
        /// <returns>The ulong ID of the channel.</returns>
        public static ulong GetBotSpamChannel(ulong serverId)
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                return DbContext.GuildLocationSettings.Where(x => x.Serverid == serverId).Select(x => x.BotSpamChannel).FirstOrDefault();
            }
        }

        /// <summary>
        /// Add a word that gets the message deleted and user warned if they say it.
        /// </summary>
        /// <param name="serverId">The guild ID to look for</param>
        /// <param name="word">The word to ban.</param>
        public async static Task AddBannedWord(ulong serverId, string word)
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                try
                {
                    BannedWords Current = new BannedWords
                    {
                        Serverid = serverId,
                        Word = word
                    };
                    DbContext.BannedWords.Add(Current);
                    
                    await DbContext.SaveChangesAsync();
                }
                catch (Exception)
                { }

            }
        }

        /// <summary>
        /// Remove a word from the banned word list
        /// </summary>
        /// <param name="serverId">The guild ID to look for</param>
        /// <param name="word">The word to remove.</param>
        public async static Task<bool> RemoveBannedWord(ulong serverId, string word)
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                try
                {   //Check if the guild has banned words
                    if (DbContext.BannedWords.Where(x => x.Serverid == serverId && x.Word == word).Count() < 1)
                    {
                        return false;
                    }
                    else
                    {
                        BannedWords Current = DbContext.BannedWords.Where(x => x.Serverid == serverId && x.Word == word).FirstOrDefault();
                        DbContext.BannedWords.Remove(Current);
                    }
                    await DbContext.SaveChangesAsync();
                    return true;
                }
                catch (Exception)
                { }
                return false;
            }
        }

        /// <summary>
        /// Sends a DM to the user containing all the banned words in the guild.
        /// </summary>
        /// <param name="serverId"></param>
        /// <returns>The string array of banned words.</returns>
        public static string[] GetBannedWords(ulong serverId)
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                //Check if the guild has banned words
                if (DbContext.BannedWords.Where(x => x.Serverid == serverId).Count() > 0)
                    return DbContext.BannedWords.Where(x => x.Serverid == serverId).Select(x => x.Word).ToArray();
                else
                    return new string[0];
            }
        }

        /// <summary>
        /// Sets the channel to send a copy of everyone message sent by users to. For keeping a log incase someone deletes their message.
        /// </summary>
        /// <param name="serverId">The guild ID to look for</param>
        /// <param name="channelId">The channel ID to post in</param>
        /// <param name="serverName">The name of the guild</param>
        public async static Task SetChatLogChannel(ulong serverId, ulong channelId, string serverName)
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                try
                {
                    //Check if the guild has a spot
                    if (DbContext.GuildLocationSettings.Where(x => x.Serverid == serverId).Count() < 1)
                    {
                        DbContext.GuildLocationSettings.Add(new GuildLocationSettings
                        {
                            Serverid = serverId,
                            WelcomeChannel = 0,
                            ServerName = serverName,
                            WelcomeMessage = "",
                            BotSpamChannel = channelId,
                            GoldInterest = 5,
                            ChatLogChannel = 0
                            
                        });
                    }
                    else
                    {
                        GuildLocationSettings Current = DbContext.GuildLocationSettings.Where(x => x.Serverid == serverId).FirstOrDefault();
                        Current.ChatLogChannel = channelId;
                        Current.ServerName = serverName;
                        DbContext.GuildLocationSettings.Update(Current);
                    }
                    await DbContext.SaveChangesAsync();
                }
                catch (Exception)
                { }
            }
        }

        /// <summary>
        /// Gets the channel to send the chat logs into.
        /// </summary>
        /// <param name="serverId">The guild ID to look for</param>
        /// <returns>A ulong ID of the channel.</returns>
        public static ulong GetChatLogChannel(ulong serverId)
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                return DbContext.GuildLocationSettings.Where(x => x.Serverid == serverId).Select(x => x.ChatLogChannel).FirstOrDefault();
            }
        }

        /// <summary>
        /// Sets a channel to send all moderator activites the bot is used for into.
        /// </summary>
        /// <param name="serverId">The guild ID to look for.</param>
        /// <param name="channelId">The channel ID to set.</param>
        /// <param name="serverName">The name of the guild.</param>
        public async static Task SetModLogChannel(ulong serverId, ulong channelId, string serverName)
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                try
                {   //Check if the guild has a spot
                    if (DbContext.GuildLocationSettings.Where(x => x.Serverid == serverId).Count() < 1)
                    {
                        DbContext.GuildLocationSettings.Add(new GuildLocationSettings
                        {
                            Serverid = serverId,
                            WelcomeChannel = 0,
                            ServerName = serverName,
                            WelcomeMessage = "",
                            BotSpamChannel = channelId,
                            GoldInterest = 5,
                            ChatLogChannel = 0,
                            ModLogChannel = 0

                        });
                    }
                    else
                    {
                        GuildLocationSettings Current = DbContext.GuildLocationSettings.Where(x => x.Serverid == serverId).FirstOrDefault();
                        Current.ModLogChannel = channelId;
                        Current.ServerName = serverName;
                        DbContext.GuildLocationSettings.Update(Current);
                    }
                    await DbContext.SaveChangesAsync();
                }
                catch (Exception)
                { }
            }
        }

        /// <summary>
        /// Gets the channel to post the mod logs into.
        /// </summary>
        /// <param name="serverId">The guild ID to look for</param>
        /// <returns>A ulong ID of the channel.</returns>
        public static ulong GetModLogChannel(ulong serverId)
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                return DbContext.GuildLocationSettings.Where(x => x.Serverid == serverId).Select(x => x.ModLogChannel).FirstOrDefault();
            }
        }

        /// <summary>
        /// Saves a custom command created in a guild.
        /// </summary>
        /// <param name="serverId">The guild ID to look for</param>
        /// <param name="destination">Where the custom command gets sent to. Either a message in a channel or a DM.</param>
        /// <param name="commandName">The name of the command</param>
        /// <param name="command">What the command does.</param>
        /// <param name="descrption">The description of the command shown in the "CustomCommands" command.</param>
        public async static Task AddCustomCommand(ulong serverId, string destination, string commandName, string command, string descrption = "")
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                try
                {   //Check if the guild has a spot
                    if (DbContext.CustomCommands.Where(x => x.Serverid == serverId && x.CommandName == commandName).Count() < 1)
                    {
                        CustomCommands Current = new CustomCommands
                        {
                            Serverid = serverId,
                            Destination = destination,
                            CommandName = commandName,
                            Command = command,
                            CommandDescription = descrption
                        };
                        DbContext.CustomCommands.Add(Current);
                    }
                    else
                    {
                        CustomCommands Current = DbContext.CustomCommands.Where(x => x.Serverid == serverId && x.CommandName == commandName).FirstOrDefault();
                        Current.Destination = destination;
                        Current.Command = command;
                    }

                    await DbContext.SaveChangesAsync();
                }
                catch (Exception)
                { }
            }
        }

        /// <summary>
        /// Saves the edited command.
        /// </summary>
        /// <param name="serverId">The guild ID to look for</param>
        /// <param name="destination">The destination for the command.</param>
        /// <param name="commandName">The old name of the command for look for in the database.</param>
        /// <param name="newCommandName">The new name of the command to save as.</param>
        /// <param name="command">What the command does.</param>
        /// <param name="commandDescription">The description of the command.</param>
        public async static Task EditCommand(ulong serverId, string destination, string commandName, string newCommandName, string command, string commandDescription)
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                CustomCommands Current = DbContext.CustomCommands.Where(x => x.Serverid == serverId && x.CommandName == commandName).FirstOrDefault();
                Current.CommandDescription = commandDescription;
                Current.Command = command;
                Current.CommandName = newCommandName;
                Current.Destination = destination;
                DbContext.CustomCommands.Update(Current);
                await DbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Removes the custom commands from the database.
        /// </summary>
        /// <param name="serverId">The guild ID to look for.</param>
        /// <param name="commandName">The name of the command to remove.</param>
        public async static Task<bool> RemoveCommand(ulong serverId, string commandName)
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                try
                {   //Check if the guild has a spot
                    if (DbContext.CustomCommands.Where(x => x.Serverid == serverId && x.CommandName == commandName).Count() < 1)
                    {
                        return false;
                    }
                    else
                    {
                        CustomCommands Current = DbContext.CustomCommands.Where(x => x.Serverid == serverId && x.CommandName == commandName).FirstOrDefault();
                        DbContext.CustomCommands.Remove(Current);
                    }
                    await DbContext.SaveChangesAsync();
                    return true;
                }
                catch (Exception)
                { }
                return false;
            }
        }

        /// <summary>
        /// Gets all of the custom commands in the guild.
        /// </summary>
        /// <param name="serverId">The guild ID to look for.</param>
        /// <returns>A list of the "CustomCommands" class that holds command information.</returns>
        public static List<CustomCommands> GetCommands(ulong serverId)
        {
            using (SqliteDbContext DbContext = new SqliteDbContext())
            {
                List<CustomCommands> results = DbContext.CustomCommands.Where(x => x.Serverid == serverId).Select(x => new CustomCommands()
                {
                    CommandName = x.CommandName,
                    Destination = x.Destination,
                    Command = x.Command,
                    CommandDescription = x.CommandDescription
                })
                .ToList();
                return results;
            }
        }
    }
}