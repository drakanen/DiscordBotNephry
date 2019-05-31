using System;
using System.Threading.Tasks;
using System.Timers;

using Discord.Commands;

namespace DiscordBot.Core.Commands
{
    public class Timers : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Creates a timer based on the time and timezone given that displays a message in the server when time is up
        /// </summary>
        /// <param name="time">The time to go off and the timezone it's in</param>
        /// <param name="reminder">The reminder to display when the timer is up</param>
        /// <example>!timer 16:30EST "Mow the lawn"</example>
        [Command("timer"), Summary("Creates a timer in the entered timezone")]
        public async Task StartTimer(string time, [Remainder]string reminder = "A timer has gone off.")
        {
            string hour = ""; //Holds the hour count
            string minute = ""; //Holds the minute count
            int hours; //int counterparts to the above strings
            int minutes;
            int index = 0; //Holds the spot on the string to look at

            try
            {
                //Get the hours in 24 hours
                for (int i = 0; i < 2; ++i)
                {
                    hour += time[index];
                    ++index;
                }

            }
            catch(Exception)
            {
                await Context.Channel.SendMessageAsync("Please make sure your time is entered correctly in 24 hour time. Such as 18:00.");
                return;
            }

            hours = int.Parse(hour); //Convert the string into an int
            
            //Make sure it's formatted correctly
            if (time[index] != ':')
            {
                await Context.Channel.SendMessageAsync("Please make sure your time is entered correctly in 24 hour time. Such as 18:00.");
                return;
            }

            ++index;

            //Get the minutes
            try
            {
                for (int i = 0; i < 2; ++i)
                {
                    minute += time[index];
                    ++index;
                }
            }
            catch(IndexOutOfRangeException)
            {
                await Context.Channel.SendMessageAsync("Please make sure your time is entered correctly in 24 hour time. Such as 18:00.");
                return;
            }

            minutes = int.Parse(minute);

            //Get the timezone part
            string timezone = "";
            try
            {
                for (int i = 0; i < 5; ++i)
                {
                    timezone += time[index];
                    ++index;
                }
            }
            catch (IndexOutOfRangeException)
            { }

            timezone = timezone.Trim().ToUpper();

            //Get the timezone
            DateTime correctTimeZone = DateTime.Today;

            //Get the current time
            DateTime now = DateTime.Now.ToUniversalTime();

            //Time until the timer goes off
            DateTime nowTime;

            //Get the timezone for the timer
            switch (timezone)
            {
                case "WAS":
                    correctTimeZone = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(correctTimeZone, "W. Australia Standard Time");
                    nowTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(now, "W. Australia Standard Time");
                    break;
                case "CAS":
                    correctTimeZone = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(correctTimeZone, "Cen. Australia Standard Time");
                    nowTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(now, "Cen. Australia Standard Time");
                    break;
                case "EAS":
                    correctTimeZone = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(correctTimeZone, "E. Australia Standard Time");
                    nowTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(now, "E. Australia Standard Time");
                    break;
                case "AST":
                    correctTimeZone = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(correctTimeZone, "Atlantic Standard Time");
                    nowTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(now, "Atlantic Standard Time");
                    break;
                case "CEST":
                    correctTimeZone = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(correctTimeZone, "Central Europe Standard Time");
                    nowTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(now, "Central Europe Standard Time");
                    break;
                case "CST":
                    correctTimeZone = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(correctTimeZone, "Central Standard Time");
                    nowTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(now, "Central Standard Time");
                    break;
                case "EEST":
                    correctTimeZone = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(correctTimeZone, "E. Europe Standard Time");
                    nowTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(now, "E. Europe Standard Time");
                    break;
                case "EST":
                    correctTimeZone = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(correctTimeZone, "Eastern Standard Time");
                    nowTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(now, "Eastern Standard Time");
                    break;
                case "GMT":
                    correctTimeZone = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(correctTimeZone, "GMT Standard Time");
                    nowTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(now, "GMT Standard Time");
                    break;
                case "HST":
                    correctTimeZone = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(correctTimeZone, "Hawaiian Standard Time");
                    nowTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(now, "Hawaiian Standard Time");
                    break;
                case "MST":
                    correctTimeZone = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(correctTimeZone, "Mountain Standard Time");
                    nowTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(now, "Mountain Standard Time");
                    break;
                case "PDT":
                    correctTimeZone = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(now, "Pacific Daylight Time");
                    nowTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(now, "Pacific Daylight Time");
                    break;
                case "PST":
                    correctTimeZone = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(correctTimeZone, "Pacific Standard Time");
                    nowTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(now, "Pacific Standard Time");
                    break;
                case "WEST":
                    correctTimeZone = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(correctTimeZone, "W. Europe Standard Time");
                    nowTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(now, "W. Europe Standard Time");
                    break;
                case "":
                    await Context.Channel.SendMessageAsync("Please make sure you enter in a timezone. \"!timer 18:00EST Make a cake\"");
                        return;
                default: await Context.Channel.SendMessageAsync($"{timezone} is not currently an available timezone.");
                    return;
            }
            
            
            // Get the correct time after the adjustment
            correctTimeZone = correctTimeZone.AddHours(hours - correctTimeZone.Hour).AddMinutes(minutes - correctTimeZone.Minute);

            // If it's already past time, wait until tomorrow    
            if (nowTime > correctTimeZone)
            {
                correctTimeZone = correctTimeZone.AddDays(1);
            }

            int msUntilTime = (int)(correctTimeZone - nowTime).TotalMilliseconds; //Time until timer goes off

            //Create the timer
            Timer timer = new Timer();
            timer.Elapsed += async (s, e) => await OnTimerEnd(reminder); //Call the OnTimerEnd method when timer ends
            timer.Enabled = true;
            timer.Interval = msUntilTime;
            timer.AutoReset = false;

            //Display the time it is set for
            await Context.Channel.SendMessageAsync($"Timer set for {correctTimeZone} {timezone}");
        }

        /// <summary>
        /// Displays the reminder set when creating the timer
        /// </summary>
        /// <param name="reminder">The reminder to send</param>
        private async Task OnTimerEnd(string reminder)
        {
            await Context.Channel.SendMessageAsync($"TIMER ALERT FOR {Context.Message.Author.Mention}: {reminder}");
        }
    }
}
