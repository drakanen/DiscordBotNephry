using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;

namespace DiscordBot.Core.Music
{
    public class PlayMusic : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Currently still being created. When finished will allow the bot to play music from any youtube link given.
        /// </summary>
        /// <param name="path">The link to the youtube video.</param>
        //[Command("play", RunMode = RunMode.Async)]
        public async Task JoinChannel(string path = "")
        {
            //Join the voice channel the user is in
            IVoiceChannel channel = (Context.User as IVoiceState).VoiceChannel;
            IAudioClient client = await channel.ConnectAsync();

            try
            {
                await SendAsync(client, path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Currently still being created. When finished will get the video to play from youtube.
        /// I don't believe it downloads the video to play yet.
        /// </summary>
        /// <param name="path">The path to the video to download.</param>
        /// <returns></returns>
        private Process CreateStream(string path)
        {
            //from youtube
             return Process.Start(new ProcessStartInfo
             {
                 FileName = "E:/Projects/DiscordBot/DiscordBot/bin/Debug/netcoreapp2.1/youtube-dl",
                 Arguments = $"-o - https://www.youtube.com/watch?v=TZRvO0S-TLU --ffmpeg-location \"E:/FFmpeg/ffmpeg.exe\" | ffmpeg -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1",
                 UseShellExecute = false,
                 RedirectStandardOutput = true,
                 CreateNoWindow = true
             });

            //From my computer
            /*return Process.Start(new ProcessStartInfo
            {
                FileName = "E:/FFmpeg/ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i {path} -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });*/
        }
        
        /// <summary>
        /// Sends audio through the discord bot when in a voice channel.
        /// </summary>
        /// <param name="client">Where to send the audio.</param>
        /// <param name="path">The video URL.</param>
        /// <returns></returns>
        private async Task SendAsync(IAudioClient client, string path)
        {
            // Create output
            using (var output = CreateStream(path).StandardOutput.BaseStream)
            using (var discord = client.CreatePCMStream(AudioApplication.Music, 128 * 1024))
            {
                try {await output.CopyToAsync(discord); }
                finally { Console.WriteLine("after"); await discord.FlushAsync().ConfigureAwait(false); }
            }
        }
    }
}
