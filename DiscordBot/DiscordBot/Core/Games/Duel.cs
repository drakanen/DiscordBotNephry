using System;
using System.Threading.Tasks;

using Discord.WebSocket;
using Discord.Commands;

using DiscordBot.Core.Utility;

using System.Collections.Concurrent;
using System.Diagnostics;

namespace DiscordBot.Core.Games
{
    public class Duel : ModuleBase<SocketCommandContext>
    {
        private static readonly ConcurrentDictionary<long, DateTimeOffset> duelResponse = new ConcurrentDictionary<long, DateTimeOffset>();

        /// <summary>
        /// Creates a duel between two contestants. The winner gets gold and the loser loses gold.
        /// If a tie happens both players lose gold.
        /// </summary>
        /// <param name="challenged">The user who is challenged to a duel.</param>
        /// <param name="money">The amount being dueled for. Defaulted at 0.</param>
        /// <returns></returns>
        [Command("duel"), Summary("Starts a duel")]
        public async Task StartDuel(SocketUser challenged, int money = 0)
        {
            //Make sure both parties can afford the duel if there's money involved
            if (money != 0)
            {
                if (Data.Data.GetGold(Context.User.Id, Context.Guild.Id) < money)
                {
                    await Context.Channel.SendMessageAsync($"{Context.User.Mention} you do not have enough money to afford this duel!");
                    return;
                }

                if (Data.Data.GetGold(challenged.Id, Context.Guild.Id) < money)
                {
                    await Context.Channel.SendMessageAsync($"{challenged.Mention} does not have enough money to afford this duel!");
                    return;
                }
            }
            //Ask the challenged to accept or decline the challenge
            if (money == 0)
                await Context.Channel.SendMessageAsync($"{challenged.Mention} you have been challenged by {Context.User.Mention} to a friendly duel!" +
                $" Type \"accept\" to accept the duel or \"decline\" to decline!");
            else
                await Context.Channel.SendMessageAsync($"{challenged.Mention} you have been challenged by {Context.User.Mention} to a duel for {money} gold!" +
                $" Type \"accept\" to accept the duel or \"decline\" to decline!");

            //Timeout timer for the response to the duel, you have 30 seconds
            Stopwatch timeout = new Stopwatch();
            timeout.Start();
            
            string answer = "";

            //Get user input
            GetUserInput input = new GetUserInput();
            await input.MainAsync(Context.Guild, challenged);

            while (answer != "accept" && answer != "decline") //While user has not answered
            {
                answer = input.answer;
                if (timeout.ElapsedMilliseconds > 30000) //If duel times out due to no response
                {
                    await Context.Channel.SendMessageAsync($"{challenged.Mention} has not responded to the duel request " +
                        $"in time so it has been canceled {Context.User.Mention}.");
                    return;
                }
            }

            //If duel is accepted
            if (answer == "accept")
            {
                int outcome = CalculateDuel(); //Get outcome of the duel
                string fight;
                if (outcome == 1) //Challenger wins the duel
                {
                    if (money == 0) //Fought for fun
                    {
                        fight = $"{Context.User.Mention} has won the duel!";
                        await Context.Channel.SendMessageAsync(fight);
                        return;
                    }
                    else
                        fight = $"{Context.User.Mention} has won the duel! Sorry {challenged.Mention}" +
                        $" but you have given up {money} gold to the victor!";

                    //Display outcome message and move gold if any was betted
                    await Context.Channel.SendMessageAsync(fight);
                    await Data.Data.SaveGold(Context.User.Id, money, Context.Guild.Id, Context.User.Username);
                    await Data.Data.SaveGoldMinus(challenged.Id, money, Context.Guild.Id, challenged.Username);
                }
                else if (outcome == 2) //Challenged wins
                {
                    if (money == 0) //If duel was fought for fun
                    {
                        fight = $"{challenged.Mention} has won the duel!";
                        await Context.Channel.SendMessageAsync(fight);
                        return;
                    }
                    else
                        fight = $"{challenged.Mention} has won the duel! Sorry {Context.User.Mention}" +
                        $" but you have given up {money} gold to the victor!";

                    //Display message and move gold if any was betted
                    await Context.Channel.SendMessageAsync(fight);
                    await Data.Data.SaveGold(challenged.Id, money, Context.Guild.Id, challenged.Username);
                    await Data.Data.SaveGoldMinus(Context.User.Id, money, Context.Guild.Id, Context.User.Username);
                }
                else if (outcome == 0) //Police got involved, both sides lose money if any was involved
                {
                    if (money == 0) //Fought for fun
                    {
                        fight = $"Uh oh! The police found you dueling and you had to spend the night in jail!";
                        await Context.Channel.SendMessageAsync(fight);
                        return;
                    }
                    else
                        fight = $"Uh oh! The police found you dueling and has confiscated {money} from both of you!";

                    //Display outcome and take money from both parties
                    await Context.Channel.SendMessageAsync(fight);
                    await Data.Data.SaveGoldMinus(challenged.Id, money, Context.Guild.Id, challenged.Username);
                    await Data.Data.SaveGoldMinus(Context.User.Id, money, Context.Guild.Id, Context.User.Username);
                }
                return;
            }
            else if (answer == "decline") //Duel is declined
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention} your duel request has been declined by {challenged.Mention}.");
                return;
            }
        }

        /// <summary>
        /// Calculate who won the duel
        /// </summary>
        /// <returns>Who won the duel. 1 = challenger wins, 2 = challenged wins, 0 = tie</returns>
        private int CalculateDuel()
        {
            int challenger;
            int challenged; 
            Random ran = new Random();
            challenger = ran.Next(10);
            challenged = ran.Next(10);

            if (challenger > challenged) //Challenger won
                return 1;
            else if (challenger < challenged) //Challenged won
                return 2;
            else
                return 0; //Police got involved
        }
    }
}