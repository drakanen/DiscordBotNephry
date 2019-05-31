using System;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace DiscordBot.Core.Commands
{
    public class Help : ModuleBase<SocketCommandContext>
    {
        [Command("help"), Alias("commands"), Summary("Displays a help menu, the menu differs depending on if the user has kick permission")]
        public async Task HelpInfo()
        {
            //Check if user issuing command is moderator+
            SocketGuildUser checkUser = Context.User as SocketGuildUser;
            if (!checkUser.GuildPermissions.KickMembers && !checkUser.GuildPermissions.Administrator)
            {
                //Send the commands available to everyone
                await SendEveryoneCommandsOnly();
                return;
            }

            //Send the entire list of commands
            await SendEntireList();
        }

        //Entire list of commands
        public async Task SendEntireList()
        {
            //Embedbuilder object
            EmbedBuilder Embed = new EmbedBuilder();

            //Add fields that contains commands
            Embed.AddField("Admin Commands 1", //Admin-only commands part 1
                "!take @user # reason - Take the specified amount of gold from the user.\n" +
                "!admingive @user # reason - Give the specified amount of gold to the user.\n" +
                "!reset @username reason - Resets the mentioned user's gold.\n" +
                "!welcomechannel (#channel) - Sets the channel for users to be welcomed in.\n" +
                "!chatlogchannel (#channel) - Sets the channel to print the chat log in.\n" +
                "!botspamchannel (#channel) - Limits the bot to talking in the specified channel.\n" +
                "!modlogchannel (#channel) - Sets the channel to print the mod log in.\n" +
                "!welcomemessage (message) - Sets the welcome message. Read next line.\n" +
                "(Use \"@newuser\" for the user that joined.)\n" +
                "!freebot - Allows the bot to respond in all channels again.");
            Embed.AddField("Admin Commands 2", //Admin-only commands part 2
                "!rolesetup - Sets up the roles for the muting system. Read the next line too.\n" +
                "(MAKE SURE BOT IS ON TOP OF THE ROLES LIST ^^^^)\n" +
                "!interest - Sets the interest rate on giving gold. \"!setinterest 5\" is 0.05%.\n" +
                "!addbannedword word(s) - Adds the word or phrase to the banned list.\n" +
                "!removebannedword word(s) - Removes the word or phrase from the banned list.\n" +
                "(anyone with kick permissions is immune to the banned words list ^^^^)\n" +
                "!stopchatlog - Stops the bot from posting in the chat log.\n" +
                "!stopmodlog - Stops the bot from posting in the mod log.\n" +
                "!addcommand - Create a custom command for use in the server.\n" +
                "(type in !customhelp for more info)\n" +
                "!editcommand cmndName - Edit the command.\n" +
                "!removecommand command - Removes the custom command"); //Commands that require the kick permission
            Embed.AddField("Moderator Commands", "!kick @username reason - Kick a user with an optional reason.\n" +
                "!ban @username reason - Ban a user with an optional reason.\n" +
                "!purge # reason - Deletes the specified number of messages in the channel.\n" +
                "!purge @username reason - Deletes all recent messages by the mentioned user.\n" +
                "!warn @username reason - Gives the specified user one warning.\n" +
                "!removewarn @username amount - Removes the specified amount of warnings.\n" +
                "!mute @username length - Mutes the user for given time or perm if left blank.\n" +
                "!unmute @username - Unmutes the user.\n" +
                "!setnickname @user name reason - Sets the users nickname and why you changed it.\n" +
                "^^^If the name you're setting it to is more than one word, surround it in quotes.\n" +
                "!createvote \"Voting For\" \"Option 1\" \"Option 2\"\n" +
                "^^^Type \"!votehelp\" for more info.\n" +
                "!raffle price limit item - Creates a raffle for 2 minutes with the item as the prize.\n" +
                "^^^Type \"!rafflehelp\" for more info.");
            Embed.AddField("Everyone Commands", //Commands available for everyone to use
                "!roll 3d2 OR !roll 3d2+1 - Rolls a dice and displays the outcome.\n" +
                "!gold @username (optional) - Displays your total amount of gold.\n" +
                "!give @username amount - Gives some of your gold to target user.\n" +
                "!store - Displays the store.\n" +
                "!blackjack # - Play blackjack with the betted amount.\n" +
                "(Type !blackjackhelp for info on how this one plays)\n" +
                "!gamble # - Gambles the amount of gold entered.\n" +
                "!guessmynumber # - Guess the my random number between 1 and 100 for gold.\n" +
                "!joindate - Display the date you joined this server for all to see.\n" +
                "!getinterest - Displays the interest rate on giving gold.\n" +
                "!bannedwords - Displays the list of banned words.\n" +
                "!topgold - Displays the top 10 users with the most gold.\n" +
                "!customcommands - Get the servers custom commands.\n" +
                "!commandName - Use the server's custom commands\n" +
                "!timer timeTIMEZONE - Sets a timer. Use is \"!timer 16:20EST\"");
            Embed.AddField("Additional help commands", "!blackjackhelp\n!customhelp\n!votehelp\n!rafflehelp");

            //Assign the author
            Embed.WithAuthor("Command list", Context.User.GetAvatarUrl());

            //Assign the color on the left side
            Embed.WithColor(40, 200, 150);
            
            await Context.User.SendMessageAsync("", false, Embed.Build()); //Send the embed to the user's DM
        }

        //Commands available to everyone
        public async Task SendEveryoneCommandsOnly()
        {
            //Embedbuilder object
            EmbedBuilder Embed = new EmbedBuilder();

            //Assign the author
            Embed.WithAuthor("Command list", Context.User.GetAvatarUrl());

            //Assign the color on the left side
            Embed.WithColor(40, 200, 150);

            Embed.AddField("Commands",
                "!roll 3d2 OR !roll 3d2+1 - Rolls a dice and displays the outcome.\n" +
                "!gold - Displays your total amount of gold.\n" +
                "!gold @username - Displays the mentioned user's amount of gold.\n" +
                "!give @username amount - Gives some of your gold to target user.\n" +
                "!store - Displays the store.\n" +
                "!gamble # - Gambles the amount of gold entered.\n" +
                "!guessmynumber # - Guess the my random number between 1 and 100 for gold.\n" +
                "!joindate - Display the date you joined this server for all to see.\n" +
                "!getinterest - Displays the interest rate on giving gold.\n" +
                "!bannedwords - Displays the list of banned words in the server.\n" +
                "!topten - Displays the top 10 users with the most gold.\n" +
                "!customcommands - Get the servers custom commands.\n" +
                "!commandName - Use the servers custom commands\n" +
                "!timer timeTIMEZONE - Sets a timer. Use is \"!timer 16:20EST Bake a cake.\"\n");
            Embed.AddField("Additional help commands", "!blackjackhelp");
            await Context.User.SendMessageAsync("", false, Embed.Build());
        }

        [Command("customhelp"), Summary("Information on how to set up and use custom commands")]
        public async Task CustomHelpInfo()
        {
            EmbedBuilder custom = new EmbedBuilder();
            custom.AddField("Destination Parsers",
                $"If you want the command to send to the channel it's used in, use `#currentchannel`\n" +
                $"If you want the command to DM whoever uses it use `@userdm`.");
            custom.AddField("Action Parsers",
                "To add a cost to the command, use `$goldcoststart` before and `$goldcoststop` after.\n" +
                "Note: Only numbers can be between the goldcost markers and they will make the bot automatically take away the money from the user.\n" +
                "If you want to display who used the command, use `$usedby`.\n");
            await Context.User.SendMessageAsync($"To create custom commands, type in: \n`!addcommand commandName`\n" +
                    $"A step-by-step will occur in the channel you use the command in to create the custom command.\n" +
                    $"You can change the command at any time with `!editcommand commandName`.\n" +
                    $"Responses to the bot to create a command that will DM you saying the person who bought a cookie for a price would be:\n" +
                    $"Destination response: `@mentionYourselfHere`\n" +
                    $"Description response: `Buy a cookie for 10 gold!`\n" +
                    $"Action response: `$usedby has bought one cooke for $goldcoststart 10 $goldcoststop gold.`", false, custom.Build());
        }

        [Command("blackjackhelp"), Summary("Information on how this blackjack game works")]
        public async Task BlackjackHelp()
        {
            await Context.User.SendMessageAsync($"This blackjack game is simplified due to me not knowing how to play it very well :(\n" +
                $"You can 'Hit', 'Stand', 'Double down', or 'Surrender' when playing.\n" +
                $"The house does not check for an ace in the hole and there are no extra side bets aside from double down.\n" +
                $"Ties go to the house and the house stands at 17.\n");
        }

        [Command("votehelp"), Summary("Information on how voting works")]
        public async Task VoteHelp()
        {
           await Context.User.SendMessageAsync("Votes are available to anyone with kick permissions.\nTo start a vote, type in: \n`!createvote \"Question\" \"Answer 1\" \"Answer 2\"`\n" +
               "Quotations around the question and answers are required. Vote lasts two minutes and gives a 30 seconds warning.\n" +
               "You can have however many answers you want until you run into discord's limit on embed field length.\n" +
               "Voting is done by typing in `vote 1` or whichever number you wish to vote for.\n" +
               "You can vote with the word instead of the number if you so desire.\n" +
               "If you are the vote creater you can stop a vote by typing `vote forcestop`, it will instantly finish the vote and display the results.\n" +
               "Or if you want to cancel the vote entirely, type `vote forcecancel`.\n" +
               "Anyone with kick permissions can also cancel or stop the vote.\n");
        }

        [Command("rafflehelp"), Summary("Information on how to make a raffle.")]
        public async Task RaffleHelp()
        {
            await Context.User.SendMessageAsync("Raffles are available for anyone with kick permissions. To create a raffle use:\n" +
                "`!raffle 10 true One golden glazed donut`\n" +
                "The \"10\" is an optional gold cost to buy a ticket to enter the raffle, it is defaulted at 0.\n" +
                "The \"true\" decides whether you want people to be able to purchase multiple tickets in the raffle.\n" +
                "True means you can purchase multiple tickets, false means only one per person. Default is false.\n" +
                "The raffle can be ended early with \"raffle end\" or canceled with \"raffle cancel\"." +
                "Ending or canceling the raffle requires the kick permission.\n");
        }
    }
}