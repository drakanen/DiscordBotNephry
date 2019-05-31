using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using DiscordBot.Core.Utility;

namespace DiscordBot.Core.Commands
{
    public class Voting : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// An inner class that defines what a Response looks like, contains the vote and voter
        /// </summary>
        private class Response
        {
            public string response; //Holds the answer to the vote
            public readonly ulong responder; //Holds who made the vote
            public Response(string vote, ulong voter)
            {
                response = vote;
                responder = voter;
            }
        }

        //Holds which guilds currently has a vote in progress, only one is allowed per guild
        private static List<ulong> currentlyInUse = new List<ulong>();

        /// <summary>
        /// Creates a vote that users can vote for in chat. It is set up through bot DMs. Requires the administrator permission.
        /// </summary>
        [Command("vote"), Summary("Allows a vote to be created with unknown amount of choices to vote for," +
            " requires quotations around the question and each answer")]
        public async Task GetVote()
        {
            SocketGuildUser userToCheck = Context.User as SocketGuildUser;
            if (!userToCheck.GuildPermissions.KickMembers)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention} you need kick permissions to create a vote.");
            }

            if (currentlyInUse.Contains(Context.Guild.Id))
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention} a vote is already on-going." +
                    $" Please wait for that one to finish before starting another.");
                return;
            }

            //Only one vote at a time per guild
            currentlyInUse.Add(Context.Guild.Id);

            //Holds the voting options
            List<string> options = new List<string>();

            //Stops the vote
            Stopwatch timeout = new Stopwatch();
            timeout.Start();
            await Context.Channel.SendMessageAsync($"{Context.User.Mention} check your DMs.");
            await Context.User.SendMessageAsync("What is being voted for.");

            //Get user input for votes
            GetUserInput dm = new GetUserInput();
            await dm.MainAsync(Context.Guild, Context.User, true, false);
            while (timeout.ElapsedMilliseconds < 60000)
            {
                if (dm.answer != "")
                {
                    options.Add(dm.answer);
                    break;
                }
            }

            if (timeout.ElapsedMilliseconds > 60000)
            {
                await Context.User.SendMessageAsync("You have timed out.");
                return;
            }

            dm.answer = "";
            timeout.Restart();
            await Context.User.SendMessageAsync("Enter the voting choices one at a time. To stop enter \"choice stop\".");
            while (timeout.ElapsedMilliseconds < 60000)
            {
                if (dm.answer != "")
                {
                    if (dm.answer == "choice stop")
                    {
                        break;
                    }
                    else
                    {
                        options.Add(dm.answer);
                        dm.answer = "";
                        await Context.User.SendMessageAsync("Choice received.");
                    }
                }
            }
            await Context.User.SendMessageAsync("Vote has been created.");

            //Create the vote
            EmbedBuilder vote;
            vote = CreateVote(options);

            try
            {   //Display the vote
                await Context.Channel.SendMessageAsync("A vote has been created! Vote with \"vote 1\".", false, vote.Build());
            }
            catch (Exception)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention} I do not have permission to post embeds in this channel!");
            }

            //Holds the responses in a list with inner class
            List<Response> responses = await GetResponses(options);
            
            if (responses[0].responder == 0)
            {
                await Context.Channel.SendMessageAsync("Vote has been canceled.");
                return;
            }

            //Get the vote's results
            int[] results = GetResults(responses, options);
            
            //Display the results
            EmbedBuilder outcome = CreateVoteResults(options, results);
            await Context.Channel.SendMessageAsync("Voting over!", false, outcome.Build());

            //Allow another vote to happen in this guild
            currentlyInUse.Remove(Context.Guild.Id);
        }
        
        /// <summary>
        /// Reads the chat for any votes being cast and records them, also allows for ending the vote early or canceling it
        /// </summary>
        /// <param name="options">What is being voted for is in index 0, choices to pick are in the rest</param>
        /// <returns>A list containing responses for the vote in the form of the innner "Response" class</returns>
        private async Task<List<Response>> GetResponses(List<string> options)
        {
            //Get user input for votes
            GetUserInput input = new GetUserInput();
            await input.MainAsync(Context.Guild, null);

            List<Response> responses = new List<Response>();

            //ID of vote starter for checking answers for stopping or canceling the vote
            ulong voteStarter = Context.User.Id;
            bool stop = false;

            string answer;
            ulong responder;

            Stopwatch timeout = new Stopwatch();
            timeout.Start();
            //Voting lasts 2 minutes
            while (timeout.ElapsedMilliseconds < 120000 && stop == false)
            {
                //Give 30 second warning before vote ends
                if (timeout.ElapsedMilliseconds == 90000)
                {   //Display the current results
                    int[] result = GetResults(responses, options);
                    EmbedBuilder outcom = CreateVoteResults(options, result);
                    await Context.Channel.SendMessageAsync("You have 30 seconds left to vote!", false, outcom.Build());
                }

                //Get the answer if there is one
                answer = input.answer;

                if (answer.StartsWith("vote"))
                {
                    responder = input.respondent; //Get the responder

                    SocketGuildUser checkUser = Context.Guild.GetUser(responder); //Check if user is moderator (has kick permissions)

                    //Check if the vote starter or a moderator wants to end it
                    if (responder == voteStarter || checkUser.GuildPermissions.KickMembers == true)
                    {
                        switch (answer)
                        {
                            case "vote stop": //Stop the vote and display the results
                                stop = true;
                                break;
                            case "vote end": //Stop the vote and display the results
                                stop = true;
                                break;
                            case "vote cancel": //Stop the vote without displaying results
                                //Allow another vote to happen in this guild
                                currentlyInUse.Remove(Context.Guild.Id);
                                return new List<Response>{ new Response("Canceled", 0) };
                        }
                    }

                    //Get rid of "vote" and leave only their pick
                    answer = answer.Remove(0, 4).Trim();

                    for (int i = 1; i < options.Count; ++i)
                    {
                        if (answer == i.ToString())
                        {
                            //If user already has a vote, change it rather than adding a new vote
                            bool exists = false;
                            foreach (Response re in responses)
                            {
                                if (re.responder == responder)
                                {
                                    re.response = answer; ;
                                    exists = true;
                                    break;
                                }
                            }

                            //Add vote if user does not already have a vote
                            if (exists == false)
                                responses.Add(new Response(answer, responder));

                            //Reset the answer
                            input.answer = "";
                            break;
                        }
                    }
                }
            }
            return responses;
        }

        /// <summary>
        /// Creates an EmbedBuilder of the vote to be displayed
        /// </summary>
        /// <param name="choices">What is being voted for is in index 0, choices to pick are in the rest</param>
        /// <returns>The EmbedBuilder containg what the vote looks like</returns>
        private EmbedBuilder CreateVote(List<string> choices)
        {
            //Build the vote embed
            EmbedBuilder vote = new EmbedBuilder();
            vote.AddField("Vote", choices[0]); //Add the question to vote for
            string options = "";
            
            //Add the options to the options string for the embed
            for (int i = 1; i < choices.Count; ++i)
            {
                options += $"[{i}] ";
                options += choices[i];
                options += "\n";
            }

            //Add the field with your voting choices
            vote.AddField("Choices", options);

            //Return the voting embed
            return vote;
        }

        /// <summary>
        /// Gets the results from the vote after it finishes
        /// </summary>
        /// <param name="responses">A list of responses made for the vote</param>
        /// <param name="options">What was being voted for in the vote</param>
        /// <returns>An int array of how many votes were given for each option</returns>
        private int[] GetResults(List<Response> responses, List<string> options)
        {
            //Holds the results
            int[] result = new int[options.Count];

            //Get the amount of votes for each choice
            foreach (Response response in responses)
            {
                for (int i = 1; i < options.Count; ++i) //Start at 1 to avoid the question, we only want the choices
                {
                    if (response.response == i.ToString())
                    { //If response matches the option, increase that index position in result
                        ++result[i];
                    }
                }
            }

            //Return the int array containing the amount of votes for each option
            return result;
        }
        
        /// <summary>
        /// Creates an EmbedBuilder holding the results of the vote
        /// </summary>
        /// <param name="options">What was being voted for and the voting options</param>
        /// <param name="results">How many votes each one got</param>
        /// <returns>The EmbedBuilder holding the results</returns>
        private EmbedBuilder CreateVoteResults(List<string> options, int[] results)
        {
            //Build the embed to display the outcome of the vote
            EmbedBuilder outcome = new EmbedBuilder();
            outcome.AddField("Vote", options[0]); //Field containing the question that was voted on

            string res = ""; //Holds the possible choices to vote for

            //Get the voting choices
            for (int i = 1; i < options.Count; ++i)
            {
                res += $"[{i}] ";
                res += options[i];
                res += " - " + results[i] + "\n";
            }

            //Add the results field holding the choices and how many votes each got
            outcome.AddField("Results", res);

            //Return the EmbedBuilder holding the voting outcome
            return outcome;
        }
    }
}
