using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;

using Discord.Commands;

using Newtonsoft.Json;

using DiscordBot.Resources.Settings;
using DiscordBot.Resources.Datatypes;

namespace DiscordBot.Core.Moderation
{
    public class Moderation : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Reloads the settings.json file without restarting the bot.
        /// </summary>
        [Command("reload"), Summary("Reload the settings.json file while the bot is running")]
        public async Task Reload()
        {
            //Checks
            if (Context.User.Id != ESettings.owner)
            {
                await Context.Channel.SendMessageAsync("You are not the owner. Ask the bot owner to execute this command!");
                return;
            }

            //Location of the settings file
            string SettingsLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location).Replace(@"bin\Debug\netcoreapp2.1\DiscordBot.dll", @"Data\Settings.json");
            if (!File.Exists(SettingsLocation))
            {
                await Context.Channel.SendMessageAsync("The file is not found in the given location. The expected location can be found in the log!");
                Console.WriteLine(SettingsLocation);
                return;
            }

            //Read the file
            string JSON = "";
            using (FileStream Stream = new FileStream(SettingsLocation, FileMode.Open, FileAccess.Read))
            using (StreamReader ReadSettings = new StreamReader(Stream))
            {
                JSON = ReadSettings.ReadToEnd();
            }

            //Update the settings
            Setting Settings = JsonConvert.DeserializeObject<Setting>(JSON);
            ESettings.banned = Settings.Banned;
            ESettings.log = Settings.Log;
            ESettings.owner = Settings.Owner;
            ESettings.token = Settings.Token;
            ESettings.version = Settings.Version;

            await Context.Channel.SendMessageAsync("Saved successfully.");
        }
    }
}
