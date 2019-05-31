using System;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using DiscordBot.Resources.Settings;

namespace DiscordBot.Core.Utility
{
    public class GetUserInput
    {
        //User Input Testing
        private DiscordSocketClient Client; //Necessary
        private SocketUser target; //Who to look for a response from
        private SocketGuild guild;
        public string answer = "";
        public ulong respondent = 0;
        private bool dm;
        private bool caseSensitive;

        /// <summary>
        /// Used to get user responses to specific questions asked by the bot.
        /// </summary>
        /// <param name="guil">The guild to look for responses in.</param>
        /// <param name="user">The user to look for responses from. If null then responses from everyone are taken.</param>
        /// <param name="DM">If the response should be through DMs or in a channel. True for DMs, false for channel.</param>
        /// <param name="caseSensitivity">Should the user's response be forced into lowercase. True forces lowercase.</param>
        public async Task MainAsync(SocketGuild guil, SocketUser user = null, bool DM = false, bool caseSensitivity = true)
        {
            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                
            }); //Empty DiscordSocketClient

            //The target
            target = user;
            guild = guil;
            dm = DM;
            caseSensitive = caseSensitivity;

            //Login
            await Client.LoginAsync(TokenType.Bot, ESettings.token);
            await Client.StartAsync();

            //Get the message
            Client.MessageReceived += Client_MessageReceived; //Called whenever someone types a message
        }

        /// <summary>
        /// Called whenever a message is recieved. Records the message sent.
        /// If not restricted to a certain person, records the responder as well.
        /// </summary>
        /// <param name="MessageParam">Information about the message sent.</param>
        #pragma warning disable CS1998 //Lacking await operator, will run synch (method is required to be async Task)
        private async Task Client_MessageReceived(SocketMessage MessageParam)
        {
            SocketUserMessage Message = MessageParam as SocketUserMessage;
            SocketCommandContext Context = new SocketCommandContext(Client, Message);

            //if message is sent by a bot, return
            if (Context.User.IsBot)
                return;

            //If message is from target return answer, else return nothing
            if (target != null && dm == false) //If it wants a certain answer from someone in a guild
            {
                if (Context.Message.Author.Id == target.Id && Context.Guild.Id == guild.Id)
                {
                    if (caseSensitive)
                        answer = MessageParam.Content.ToLower();
                    else
                        answer = MessageParam.Content;
                }
            }
            else if (target != null && dm == true) //If it's searching for a DM by a specific person
            {
                if (Context.Message.Author.Id == target.Id && Context.IsPrivate)
                {
                    if (caseSensitive)
                        answer = MessageParam.Content.ToLower();
                    else
                        answer = MessageParam.Content;
                }
            }
            else //Get answers from everyone
            {
                respondent = MessageParam.Author.Id;
                if (caseSensitive)
                    answer = MessageParam.Content.ToLower();
                else
                    answer = MessageParam.Content;
            }
        }
    }
}
