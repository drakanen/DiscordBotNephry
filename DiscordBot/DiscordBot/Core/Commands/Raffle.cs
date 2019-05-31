using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace DiscordBot.Core.Commands
{
    public class Raffle : ModuleBase<SocketCommandContext>
    {
        //Holds which guilds currently have a raffle in progress, only one is allowed per guild
        private static List<ulong> currentlyInUse = new List<ulong>();

        /// <summary>
        /// Creates a raffle to be held in the server, allows adding a price on tickets, how many tickets can be bought, and what is being raffled for.
        /// Requires the administrator permission.
        /// </summary>
        [Command("raffle"), Summary("Creates a raffle with optional ticket price and multiple or single tickets per person allowed")]
        public async Task CreateRaffle()
        {
            //Make sure the user has kick permissions
            SocketGuildUser checkForPerms = Context.User as SocketGuildUser;
            if (!checkForPerms.GuildPermissions.KickMembers)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention} you need kick permissions to create raffles.");
                return;
            }

            //Checks if a raffle is currently going on in the server
            if (currentlyInUse.Contains(Context.Guild.Id))
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention} a raffle is already on-going." +
                    $" Please wait until the raffle is finished before starting another.");
                return;
            }

            //Only one raffle per guild at a time
            currentlyInUse.Add(Context.Guild.Id);

            //Get user input through DM
            Core.Utility.GetUserInput dm = new Core.Utility.GetUserInput();
            await dm.MainAsync(Context.Guild, Context.User, true, false);
            await Context.Channel.SendMessageAsync($"{Context.User.Mention} please check your DMs.");


            //Time out setting the raffle if the user takes too long
            Stopwatch timeout = new Stopwatch();
            timeout.Start();

            //Get the item being raffled off
            await Context.User.SendMessageAsync("What would you like to raffle");
            string raffledItem = "";
            while (timeout.ElapsedMilliseconds < 30000)
            {
                if (dm.answer != "")
                {
                    raffledItem = dm.answer;
                    break;
                }
            }

            //If they timed out, tell them so and return
            if (timeout.ElapsedMilliseconds > 30000)
            {
                await Context.User.SendMessageAsync("You have timed out.");
                return;
            }

            dm.answer = "";
            timeout.Restart();
            //Get the price of a ticket
            await Context.User.SendMessageAsync("What is the price of a ticket? 0 for free.");
            int price = 0;
            while (timeout.ElapsedMilliseconds < 30000) //While the the user hasn't timed out
            {
                if (dm.answer != "")
                {
                    try { price = int.Parse(dm.answer);
                        if (price > -1) //If price is acceptable, assign it and break out of the while loop
                            break; 
                        else
                        {
                            await Context.User.SendMessageAsync("Please enter a non-negative number");
                            dm.answer = "";
                        }
                    }
                    catch (FormatException) {
                        await Context.User.SendMessageAsync("Please enter a non-negative number");
                        dm.answer = "";

                    }
                }
            }

            //Tell the user if they timed out and return
            if (timeout.ElapsedMilliseconds > 30000)
            {
                await Context.User.SendMessageAsync("You have timed out.");
                return;
            }

            dm.answer = "";
            timeout.Restart();
            //Ask if you can buy multiple tickets
            await Context.User.SendMessageAsync("How many tickets can you buy? 0 for unlimited");
            int amountOfEntries = 0;
            while (timeout.ElapsedMilliseconds < 30000)
            {
                if (dm.answer != "")
                {
                    try
                    {
                        amountOfEntries = int.Parse(dm.answer);
                        if (amountOfEntries > -1) //If the amount of tickets you can purchase is acceptable, break out of the loop
                            break;
                        else
                        {
                            await Context.User.SendMessageAsync("Please enter a non-negative number");
                            dm.answer = "";
                        }
                    }
                    catch (FormatException) {
                        await Context.User.SendMessageAsync("Please enter a non-negative number");
                        dm.answer = "";
                    }
                }
            }
            //If the user times out, tell them and return
            if (timeout.ElapsedMilliseconds > 30000)
            {
                await Context.User.SendMessageAsync("You have timed out.");
                return;
            }

            await Context.User.SendMessageAsync("Raffle has been created.");
            timeout.Restart();

            //Display the raffle
            EmbedBuilder raffle = GetRaffleToDisplay(price, amountOfEntries, raffledItem);
            await Context.Channel.SendMessageAsync("A raffle has been created! Enter \"raffle join amount\" where \"amount\"" +
                " is how many tickets you want to buy to join!", false, raffle.Build());
            
            //Host the raffle
            List<ulong> participants = await GetEntries(price, amountOfEntries, raffle);
            
            
            //If at least one person responded
            if (participants.Count > 0)
            {
                if (participants[0] == 0)
                    return;

                ulong winner = GetWinner(participants); //Get a winner
                await Context.Channel.SendMessageAsync($"The raffle for \"{raffledItem}\" has ended. {Context.Guild.GetUser(winner).Mention} has won!" +
                    $" Please contact {Context.User.Mention} to get your reward!"); //Display the winner
            }
            else
                await Context.Channel.SendMessageAsync("The raffle has ended with no participants.");

            //Allow another raffle to happen in this guild
            currentlyInUse.Remove(Context.Guild.Id);
        }
        
        /// <summary>
        /// Gets the raffle to be displayed in the server chat
        /// </summary>
        /// <param name="price">The cost of one ticket</param>
        /// <param name="amountOfTickets">How many tickets one person can buy</param>
        /// <param name="item">The item that is being raffled</param>
        /// <returns>EmbedBuilder holding the raffle to be displayed</returns>
        private EmbedBuilder GetRaffleToDisplay(int price, int amountOfTickets, string item)
        {
            //Create an embed to show the raffle details
            Discord.EmbedBuilder raffle = new Discord.EmbedBuilder();
            Random ran = new Random(); //Used to get a random number for the color of the embed
            int color1 = ran.Next(0, 256);
            int color2 = ran.Next(0, 256); //Get three random numbers for the color
            int color3 = ran.Next(0, 256);
            raffle.WithColor(color1, color2, color3); //Add a color to the raffle with RBG values
            raffle.AddField("Prize", item); //Add the field holding the prize

            //Display that a raffle was made
            if (price == 0) //If there is a price per ticket
                raffle.AddField("Entry Cost", "Free");
            else
                raffle.AddField("Entry Cost", price + " gold");

            if (amountOfTickets > 0) //How many tickets you can purchase
                raffle.AddField("Number Of Tickets Per Person", amountOfTickets);
            else
                raffle.AddField("Number Of Tickets Per Person", "Unlimited");

            return raffle;
        }
        
        /// <summary>
        /// Allows users to purchase tickets for the on-going raffle
        /// </summary>
        /// <param name="price">The cost of a ticket as an int</param>
        /// <param name="amountOfTickets">The amount of tickets one user can purchase as an int</param>
        /// <param name="raffle">The raffle details contained in an EmbedBuilder</param>
        /// <returns>A list of participants in the raffle as a ulong</returns>
        private async Task<List<ulong>> GetEntries(int price, int amountOfTickets, EmbedBuilder raffle)
        {
            //Hold the participants in the raffle
            List<ulong> participants = new List<ulong>();

            //Stop the raffle after 2 minutes
            Stopwatch timeout = new Stopwatch();
            timeout.Start();

            //Get user input
            Core.Utility.GetUserInput input = new Core.Utility.GetUserInput();
            await input.MainAsync(Context.Guild, null);

            int tickets = 0; //Number of tickets that have been purchased
            bool endRaffle = false; //If raffle is stopped early

            //Used to take gold and check for kick permissions to end raffle early
            SocketGuildUser userToCheck;

            //Hold the raffle for 2 minutes unless stopped early
            while (timeout.ElapsedMilliseconds < 120000 && !endRaffle)
            {
                if (timeout.ElapsedMilliseconds == 90000)
                {
                    raffle.AddField("Number of Tickets Bought", tickets);
                    await Context.Channel.SendMessageAsync($"The raffle has 30 seconds left!", false, raffle.Build());
                }

                if (input.answer != "")
                {
                    if (input.answer.StartsWith("raffle join"))//User wants to join the raffle
                    {
                        userToCheck = Context.Guild.GetUser(input.respondent); //Get the user
                        int amountToBuy = 1;
                        try
                        {
                            int length = input.answer.Length - 11;
                            if (length > 0)
                            {
                                amountToBuy = int.Parse(input.answer.Substring(11, length).Trim());
                            }
                        }
                        catch (FormatException) { }
                        catch (ArgumentNullException) { }

                        if (price != 0) //If there is a price for each ticket
                        {
                            int totalCost = amountToBuy * price;
                            if (Data.Data.GetGold(userToCheck.Id, Context.Guild.Id) < totalCost) //Make sure they have enough gold to buy a ticket
                            {
                                await Context.Channel.SendMessageAsync($"{userToCheck.Mention} you do not have enough gold to buy {amountToBuy} ticket(s).");
                            }
                            else
                            {
                                if (amountOfTickets == 0) //Unlimited amount of tickets
                                {
                                    await Data.Data.SaveGoldMinus(userToCheck.Id, totalCost, Context.Guild.Id);
                                    for (int i = 0; i < amountToBuy; ++i)
                                    {
                                        participants.Add(input.respondent);
                                        ++tickets;
                                    }
                                    await Context.Channel.SendMessageAsync($"{userToCheck.Mention} you have successfully bought {amountToBuy} ticket(s)!");
                                }
                                else //Limited amount of tickets
                                {
                                    int amountOfTicketsOwned = 0;
                                    foreach (ulong particpant in participants)
                                    {
                                        if (particpant == input.respondent)
                                            ++amountOfTicketsOwned;
                                    }

                                    if (amountOfTicketsOwned + amountToBuy < amountOfTickets)
                                    {
                                        await Data.Data.SaveGoldMinus(userToCheck.Id, totalCost, Context.Guild.Id);
                                        for (int i = 0; i < amountToBuy; ++i)
                                        {
                                            participants.Add(input.respondent);
                                            ++tickets;
                                        }
                                        await Context.Channel.SendMessageAsync($"{userToCheck.Mention} you have successfully bought {amountToBuy} ticket(s)!");
                                    }
                                    else
                                        await Context.Channel.SendMessageAsync($"{userToCheck.Mention} you cannot have that many tickets!");
                                }
                            }
                        }
                        else
                        {
                            if (amountOfTickets == 0) //Unlimited amount of tickets
                            {
                                for (int i = 0; i < amountToBuy; ++i)
                                {
                                    participants.Add(input.respondent);
                                    ++tickets;
                                }
                                await Context.Channel.SendMessageAsync($"{userToCheck.Mention} you have successfully bought {amountToBuy} ticket(s)!");
                            }
                            else //Limited amount of tickets
                            {
                                int amountOfTicketsOwned = 0;
                                foreach (ulong particpant in participants)
                                {
                                    if (particpant == input.respondent)
                                        ++amountOfTicketsOwned;
                                }
                                if (amountOfTicketsOwned + amountToBuy <= amountOfTickets)
                                {
                                    for (int i = 0; i < amountToBuy; ++i)
                                    {
                                        participants.Add(input.respondent);
                                        ++tickets;
                                    }
                                    await Context.Channel.SendMessageAsync($"{userToCheck.Mention} you have successfully bought {amountToBuy} ticket(s)!");
                                }
                                else
                                    await Context.Channel.SendMessageAsync($"{userToCheck.Mention} you cannot have that many tickets!");
                            }
                        }
                    }
                    else if (input.answer == "raffle cancel") //Cancel the raffle
                    {
                        userToCheck = Context.Guild.GetUser(input.respondent);
                        if (userToCheck.GuildPermissions.KickMembers == true)
                        {
                            endRaffle = true;
                            await Context.Channel.SendMessageAsync($"{userToCheck.Mention} the raffle has been successfully canceled." +
                                $" Everyone who entered has been refunded.");

                            //Refund everyones money
                            await Data.Data.SaveGoldMass(participants.ToArray(), price, Context.Guild.Id);

                            //Allow another raffle to happen in this guild
                            currentlyInUse.Remove(Context.Guild.Id);
                            return new List<ulong>(new ulong[] { 0 });
                        }
                    }
                    else if (input.answer == "raffle end") //Stop the raffle early and pick a winner
                    {
                        userToCheck = Context.Guild.GetUser(input.respondent);
                        if (userToCheck.GuildPermissions.KickMembers == true)
                            endRaffle = true;
                    }
                    input.answer = "";
                }
            }
            return participants;
        }

        /// <summary>
        /// Picks a winner for the raffle from a list of ulongs
        /// </summary>
        /// <param name="contestors">A list of ulongs containing the participants in the raffle</param>
        /// <returns>The ulong of the winner</returns>
        private ulong GetWinner(List<ulong> contestors)
        {
            Random random = new Random();
            int winner = random.Next(0, contestors.Count);
            return contestors[winner];
        }
    }
}
