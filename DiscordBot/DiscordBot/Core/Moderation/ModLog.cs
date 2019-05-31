using System;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace DiscordBot.Core.Moderation
{
    public static class ModLog
    {
        /// <summary>
        /// Posts any moderation actions done with the bot into the assigned mod log channel if one is assigned.
        /// Post is an embed that shares the action, target'd user, user that used the command, and reason for the command.
        /// </summary>
        /// <param name="guild">The guild the command was used in.</param>
        /// <param name="action">The moderation action that was used.</param>
        /// <param name="user">The moderator who used the command.</param>
        /// <param name="target">The user targeted by the command.</param>
        /// <param name="reason">The reason for using the command.</param>
        /// <returns></returns>
        public static async Task PostInModLog(SocketGuild guild, string action, SocketUser user, IGuildUser target, [Remainder]string reason)
        {
            //Channel to send the chat logs in
            ulong channel = Data.Data.GetModLogChannel(guild.Id);
            if (channel != 0) //If a channel has been assigned as the mod log channel
            {
                Random ran = new Random();
                int color1 = ran.Next(0, 256); //Gets a random color for the embed
                int color2 = ran.Next(0, 256);
                int color3 = ran.Next(0, 256);
                SocketTextChannel channelPost = guild.GetTextChannel(channel);
                if (target != null) //If action was targeted at a user
                {
                    //Embedbuilder object
                    EmbedBuilder Embed = new EmbedBuilder();
                    Embed.AddField("Action:", action);
                    Embed.AddField("Targeted User:", $"{target.Username} ({target.Mention})");
                    Embed.AddField("Moderator:", $"{user.Username} ({user.Mention})");
                    Embed.AddField("Reason:", reason);

                    //Assign the author's image
                    Embed.WithThumbnailUrl(user.GetAvatarUrl());

                    //Add the timestamp to the bottom
                    Embed.WithCurrentTimestamp();

                    //Assign the color on the left side
                    Embed.WithColor(color1, color2, color3);

                    try
                    {
                        await channelPost.SendMessageAsync("", false, Embed.Build());
                    }
                    catch (Exception)
                    {
                        await user.SendMessageAsync("I do not have permission to post in the mod log. Please inform the administrator of the server.");
                    }
                }
                else //If action was not targeted at a user
                {
                    //Embedbuilder object
                    EmbedBuilder Embed = new EmbedBuilder();
                    Embed.AddField("Action:", action);
                    Embed.AddField("Moderator:", $"{user.Username} ({user.Mention})");

                    if (reason != "")
                        Embed.AddField("Reason:", reason);

                    //Assign the author's image
                    Embed.WithThumbnailUrl(user.GetAvatarUrl());
                    Embed.WithCurrentTimestamp();

                    //Assign the color on the left side
                    Embed.WithColor(color1, color2, color3);
                    try
                    {
                        await channelPost.SendMessageAsync("", false, Embed.Build());
                    }
                    catch (Exception)
                    {
                        await user.SendMessageAsync("I do not have permission to post in the mod log. Please inform the administrator of the server.");
                    }

                }  
                
            }
        }
    }
}
