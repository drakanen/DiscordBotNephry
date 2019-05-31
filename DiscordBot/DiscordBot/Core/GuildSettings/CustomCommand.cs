using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using DiscordBot.Core.Utility;
using DiscordBot.Resources.Database;

namespace DiscordBot.Core.GuildSettings
{
    public class CustomCommand : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Creates a custom command with user interaction in the channel for the specific guild.
        /// </summary>
        /// <param name="name">The name of the command to add.</param>
        [Command("addcommand"), Summary("Add a guild specific custom command with specific parameters for customization")]
        private async Task AddCommand([Remainder]string name = null)
        {
            //Test if the user using the command has the kick or administrator permission
            SocketGuildUser checkUser = Context.User as SocketGuildUser;
            if (!checkUser.GuildPermissions.Administrator && checkUser.Id != 187644003091480577) //Make sure user issuing command has permissions
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, You cannot add custom commands, this requires you to be an administrator.");
                return;
            }

            //User input
            GetUserInput input = new GetUserInput();
            await input.MainAsync(Context.Guild, Context.User);

            //Cancel after 60 seconds
            Stopwatch timeout = new Stopwatch();
            timeout.Start();
            int timeoutTime = 60000;
            input.answer = null;

            if (name == null)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention} what will the name of this command be?");
                while (timeout.ElapsedMilliseconds < timeoutTime)
                {
                    if (input.answer != null)
                    {
                        name = input.answer;
                        break;
                    }
                }
            }

            await Context.Channel.SendMessageAsync($"{Context.User.Mention} where would you like this command to send to? Use @userdm if you want it to DM whoever uses it or #currentchannel " +
                "for whichever channel it's used in. Otherwise just mention the user or channel to send to.");

            input.answer = "";

            string destination = ""; //Holds the destination

            //Get the destination
            while (timeout.ElapsedMilliseconds < timeoutTime)
            {
                if (input.answer != "")
                {
                    destination = input.answer;
                    break;
                }
            }
            input.answer = "";
            //Make sure destination is either a mention or channel
            if (destination.Contains('@') || destination.Contains('#'))
            {
                //Get a description for the command
                await Context.Channel.SendMessageAsync($"{Context.User.Mention} please type in a description for {name}.");
                string description = "";

                timeout.Restart();
                //Search for an answer to the description
                while (timeout.ElapsedMilliseconds < 60000)
                {
                    if (input.answer != "")
                    {
                        description = input.answer;
                        break;
                    }
                }

                //If no description is entered
                if (description == "")
                {
                    await Context.Channel.SendMessageAsync($"{Context.User.Mention} no description was entered so the command has not been created.");
                    return;
                }

                await Context.Channel.SendMessageAsync($"{Context.User.Mention} please type in the action you want to happen, such as saying \"Hello\" in chat. " +
                    $"If you need help with parse commands such as mentioning who used the command or adding a cost to this action, please use \"!customhelp\"");

                string action = "";

                input.answer = "";
                timeout.Restart();
                //Search for an answer to the description
                while (timeout.ElapsedMilliseconds < 60000)
                {
                    if (input.answer != "" && input.answer != "!customhelp")
                    {
                        action = input.answer;
                        break;
                    }
                }

                //If no description is entered
                if (action == "")
                {
                    await Context.Channel.SendMessageAsync($"{Context.User.Mention} no action was entered so the command has not been created.");
                    return;
                }
                //Add the command
                await Data.Data.AddCustomCommand(Context.Guild.Id, destination, name.ToLower(), action, description);

                await Context.Channel.SendMessageAsync($"{Context.User.Mention} \"{name}\" was successfully added to the custom command list.");
            }
            else
                await Context.Channel.SendMessageAsync($"{Context.User.Mention} please make sure the command is formatted correctly. Use !customhelp if you need to.");
            }

        /// <summary>
        /// Edits a command using DM interactions.
        /// </summary>
        /// <param name="commandName">The name of the command to edit.</param>
        /// <returns></returns>
        [Command("editcommand"), Summary("Edit a command")]
        private async Task EditCommand([Remainder]string commandName = null)
        {
            //Test if the user using the command has the kick or administrator permission
            SocketGuildUser checkUser = Context.User as SocketGuildUser;
            if (!checkUser.GuildPermissions.Administrator) //Make sure user issuing command has permissions
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you cannot edit descriptions, this requires you to be an administrator.");
                return;
            }

            if (commandName == null)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention} please give a command to edit. \"!editcommand say hello\"");
            }
            //If the guild has custom commands
            if (Data.Data.GetCommands(Context.Guild.Id).Count > 0)
            {
                //Get a list of all commands
                List<CustomCommands> commandList = Data.Data.GetCommands(Context.Guild.Id);
                foreach (CustomCommands command in commandList)
                {   //Check for a matching command name
                    if (command.CommandName == commandName.ToLower())
                    {
                        string newName = command.CommandName; //Holds the updated information for the command
                        string newDestination = command.Destination;
                        string newDescription = command.CommandDescription;
                        string newAction = command.Command;

                        //Tell the user to check their DMs
                        await Context.Channel.SendMessageAsync($"{Context.User.Mention} the \"{commandName}\" command has been found.\n" +
                            "Please check your DMs.");

                        //Display the editing menu
                        await DisplayEditMenu(Context.User);

                        //Get user input
                        GetUserInput input = new GetUserInput();
                        await input.MainAsync(Context.Guild, Context.User, true);

                        //Timeout the user if they take too long
                        Stopwatch timeout = new Stopwatch();
                        timeout.Start();

                        int timeoutTime = 30000; //How long before timeout

                        //While they do not exit the menu and time does not run out
                        while (timeout.ElapsedMilliseconds < timeoutTime)
                        {
                            switch (input.answer) //Switch-case for their string answer
                            {
                                case "1":
                                    timeout.Restart(); //Restart the timeout timer
                                    input.answer = ""; //Reset the input
                                    await Context.User.SendMessageAsync("Please enter in the new name for the command.");
                                    while (timeout.ElapsedMilliseconds < timeoutTime) //While time does not run out
                                    {
                                        if (input.answer != "")
                                        {
                                            newName = input.answer; //Get their answer and break out of the while loop
                                            break;
                                        }
                                    }
                                    await DisplayEditMenu(Context.User); //Display menu again
                                    timeout.Restart(); //Restart the timeout timer
                                    input.answer = ""; //Reset their answer
                                    break;
                                case "2":
                                    timeout.Restart();
                                    input.answer = "";
                                    await Context.User.SendMessageAsync("Please enter in the new description for the command.");
                                    while (timeout.ElapsedMilliseconds < timeoutTime) //While time does not run out
                                    {
                                        if (input.answer != "")
                                        {
                                            newDescription = input.answer; //Get their answer and break out of the while loop
                                            break;
                                        }
                                    }
                                    await DisplayEditMenu(Context.User); //Display menu again
                                    timeout.Restart(); //Restart the timeout timer
                                    input.answer = ""; //Reset their answer
                                    break;
                                case "3":
                                    timeout.Restart();
                                    input.answer = "";
                                    await Context.User.SendMessageAsync("Please enter in the new destination for the command.");
                                    while (timeout.ElapsedMilliseconds < timeoutTime) //While time does not run out
                                    {
                                        if (input.answer != "")
                                        {
                                            newDestination = input.answer; //Get their answer and break out of the while loop
                                            break;
                                        }
                                    }
                                    await DisplayEditMenu(Context.User); //Display menu again
                                    timeout.Restart(); //Restart the timeout timer
                                    input.answer = ""; //Reset their answer
                                    break;
                                case "4":
                                    timeout.Restart();
                                    input.answer = "";
                                    await Context.User.SendMessageAsync("Please enter in the new action for the command.");
                                        
                                    while (timeout.ElapsedMilliseconds < timeoutTime) //While time does not run out
                                    {
                                        if (input.answer != "")
                                        {
                                            newAction = input.answer; //Get their answer and break out of the while loop
                                            break;
                                        }
                                    }
                                    await DisplayEditMenu(Context.User); //Display menu again
                                    timeout.Restart(); //Restart the timeout timer
                                    input.answer = ""; //Reset their answer
                                    break;
                                case "5":
                                    await Data.Data.EditCommand(Context.Guild.Id, newDestination, command.CommandName, newName, newAction, newDescription);
                                    await Context.User.SendMessageAsync($"Command has been successfully updated.");
                                    timeout.Restart();
                                    break;
                                case "6":
                                    await Data.Data.EditCommand(Context.Guild.Id, newDestination, command.CommandName, newName, newAction, newDescription);
                                    await Context.User.SendMessageAsync($"Command has been successfully updated and exited.");
                                    return;
                                case "7":
                                    await Context.User.SendMessageAsync($"Exited without saving.");
                                    return;
                            }
                        }
                        timeout.Stop(); //Stop the timer

                        //Tell them if they've been timed out
                        if (timeout.ElapsedMilliseconds > timeoutTime)
                            await Context.User.SendMessageAsync($"{Context.User.Mention} you have been timed out. Any unsaved progress has been lost.");
                    }
                }
                await Context.Channel.SendMessageAsync($"{commandName} has not been found. Are you sure it is a valid command and has been spelled correctly?");
            }
            else
                await Context.Channel.SendMessageAsync($"{Context.User.Mention} this server has no commands to add a description to!");
        }

        /// <summary>
        /// Remove the command from the database
        /// </summary>
        /// <param name="name">The name of the command to remove</param>
        /// <returns></returns>
        [Command("removecommand"), Summary("Remove a command")]
        private async Task RemoveCommand([Remainder]string name = null)
        {
            //Test if the user using the command has the kick or administrator permission
            SocketGuildUser checkUser = Context.User as SocketGuildUser;
            if (!checkUser.GuildPermissions.Administrator && checkUser.Id != 187644003091480577) //Make sure user issuing command is admin
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, You cannot remove custom commands, this requires you to be an administrator.");
                return;
            }

            if (name == null)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention} you have to specify what command to remove.");
                return;
            }

            //Try to remove the command
            bool result;
            result = await Data.Data.RemoveCommand(Context.Guild.Id, name.ToLower());

            if (result) //If successfully removed the command
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, \"{name}\" was successfully removed from the custom command list.");
            else
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, \"{name}\" was not found in the custom command list. Are you sure it was spelled correctly?");
        }

        /// <summary>
        /// Sends a DM containing an embed with the guild's custom commands.
        /// </summary>
        [Command("customcommands"), Summary("Get custom commands")]
        private async Task GetCommands()
        {
            if (Data.Data.GetCommands(Context.Guild.Id).Count > 0)
            {
                List<CustomCommands> commandList = Data.Data.GetCommands(Context.Guild.Id);
                string commands = $"Custom commands in {Context.Guild.Name}: \n";
                foreach (CustomCommands command in commandList)
                {
                    commands += "!" + command.CommandName + " - ";
                    commands += command.CommandDescription + "\n";
                }

                //Embedbuilder object
                EmbedBuilder Embed = new EmbedBuilder();

                //Assign the author
                Embed.WithAuthor("Custom Commands", Context.User.GetAvatarUrl());

                //Assign the color on the left side
                Embed.WithColor(40, 200, 150);

                //Create the description
                Embed.WithDescription(commands);
                await Context.User.SendMessageAsync("", false, Embed.Build());
            }
            else
                await Context.Channel.SendMessageAsync("There are no custom commands in this server.");
        }
        
        /// <summary>
        /// Called from the Main class when a hard coded command isn't found.
        /// Uses the command being called if it's found.
        /// </summary>
        /// <param name="Context">Information about where the command is being used and what the command is.</param>
        public async Task UseCustomCommand(SocketCommandContext Context)
        {
            //Get all of the server's custom commands
            List<CustomCommands> commandList = Data.Data.GetCommands(Context.Guild.Id);
            string commands = $"Custom commands in {Context.Guild.Name}: \n";

            //Search for the matching command
            foreach (CustomCommands command in commandList)
            {
                //When found
                if (command.CommandName == Context.Message.Content.Remove(0, 1).ToLower())
                {
                    //Get the destination to send the response (either DM or channel)
                    string destination = command.Destination;
                    ulong destinationUlong;
                    SocketUser target = null; //Holds the user to DM it to if it goes to a user
                    ISocketMessageChannel targetChannel = null; //Holds the channel to send it to if it's a channel
                    if (destination.Contains('@'))
                    {
                        //If it should target whoever uses the command
                        if (destination.Contains("@userdm"))
                        {
                            target = Context.User;
                        }
                        else
                        {   //Target a specific person
                            if (destination.Contains('!'))
                                destination = destination.Remove(0, 3);
                            else
                                destination = destination.Remove(0, 2);
                            destination = destination.Remove(destination.Length - 1);

                            destinationUlong = ulong.Parse(destination);
                            target = Context.Guild.GetUser(destinationUlong);
                        }
                    }
                    else if (destination.Contains('#'))
                    {
                        if (destination.Contains("#currentchannel"))
                            targetChannel = Context.Channel;
                        else
                        {
                            //Get the channel to send the message to
                            destination = destination.Remove(0, 2);
                            destination = destination.Remove(destination.Length - 1);

                            destinationUlong = ulong.Parse(destination);
                            targetChannel = Context.Guild.GetTextChannel(destinationUlong);
                        }
                    }
                    //Get the command string
                    string customcommand = command.Command;

                    //Replace who sent the command if needed
                    if (customcommand.Contains("$usedby"))
                    {
                        customcommand = customcommand.Replace("$usedby", Context.User.Mention);
                    }

                    //Search for a cost to use the command
                    if (customcommand.Contains("$goldcost"))
                    {
                        //Get the gold cost
                        int amount = GetGoldCost(customcommand);

                        //Make sure they have the amount needed
                        if (amount > Data.Data.GetGold(Context.User.Id, Context.Guild.Id))
                        {
                            await Context.Channel.SendMessageAsync($"{Context.User.Mention} you do not have enough gold to use this command!");
                            return;
                        }

                        //Take the gold away
                        await Data.Data.SaveGoldMinus(Context.User.Id, amount, Context.Guild.Id, Context.User.Username);

                        //Replace the gold cost parameters with the cost
                        customcommand = ReplaceGoldCost(customcommand, amount.ToString());
                    }

                    try
                    {
                        //Send message to target's DM
                        if (target != null)
                            await target.SendMessageAsync(customcommand + "\n");
                        else //Send message to target channel
                            await targetChannel.SendMessageAsync(customcommand);
                    }
                    catch (Exception)
                    {
                        await Context.Channel.SendMessageAsync($"{Context.User.Mention} the destination for this command does not exist. Please contact " +
                            $"an administrator to fix this command.");
                        return;
                    }

                    if (targetChannel != Context.Channel)
                        await Context.Channel.SendMessageAsync($"{Context.User.Mention}, the command has been used successfully.");

                    return;
                }
            }
        }

        /// <summary>
        /// Gets the gold cost to use the command if there is one.
        /// </summary>
        /// <param name="customcommand">What the command does</param>
        /// <returns>The gold cost for the command</returns>
        private int GetGoldCost(string customcommand)
        {
            if (customcommand.Contains("$goldcost"))
            {
                //Get the gold cost
                int amountStart = customcommand.IndexOf("$goldcoststart") + 14;
                int amountEnd = customcommand.IndexOf("$goldcoststop", amountStart);
                string total = customcommand.Substring(amountStart, amountEnd - amountStart);
                total = total.Trim();
                return int.Parse(total);
            }
            else
                return -1;
        }

        /// <summary>
        /// Replaces the parsers for getting the gold cost when displaying custom commands.
        /// </summary>
        /// <param name="customcommand">The command</param>
        /// <param name="total"></param>
        /// <returns></returns>
        private string ReplaceGoldCost(string customcommand, string total)
        {
            if (customcommand.Contains("$goldcost"))
            {
                //Replace the gold cost parameters with the cost
                int start = customcommand.IndexOf("$goldcoststart");
                int end = customcommand.IndexOf("$goldcoststop", start) + 13;
                string replace = customcommand.Substring(start, end - start);
                return customcommand.Replace(replace, total);
            }
            else
                return customcommand;
        }

        /// <summary>
        /// Display the menu to the user when they are editting a command.
        /// </summary>
        /// <param name="user">The user to send the menu to.</param>
        private async Task DisplayEditMenu(SocketUser user)
        {
            await user.SendMessageAsync($"Please enter the number for an option from below to edit:\n" +
                            $"1 - Name\n" +
                            $"2 - Description\n" +
                            $"3 - Destination\n" +
                            $"4 - Action\n" +
                            $"5 - Save" +
                            $"6 - Save And Exit" +
                            $"7 - Exit Without Saving");
        }
    }
}