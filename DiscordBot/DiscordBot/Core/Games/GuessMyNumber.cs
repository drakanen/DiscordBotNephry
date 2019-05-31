using System;
using System.Threading.Tasks;

using Discord.Commands;
using System.Collections.Concurrent;

namespace DiscordBot.Core.Games
{
    class GuessMyNumber : ModuleBase<SocketCommandContext>
    {
        //Used for the cooldown timer
        private static readonly ConcurrentDictionary<long, DateTimeOffset> guessLastCall = new ConcurrentDictionary<long, DateTimeOffset>();

        /// <summary>
        /// Generates a random number that the user guesses. If they guess right they earn 50 gold. A wrong guess is worth 2 gold.
        /// </summary>
        /// <param name="guess">The guess being made on the number.</param>
        [Command("guessmynumber"), Alias("pickmynumber"), Summary("Game where you try and guess a random number between 1 and 100")]
        public async Task GuessNumber(int guess = 0)
        {
            ulong id = Context.User.Id; //Cooldown information
            DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
            bool canReturnGold = true;

            //Cool down timer
            guessLastCall.AddOrUpdate((long)id, nowUtc, (key, oldValue) =>
            {
                TimeSpan elapsed = nowUtc - oldValue;
                if (elapsed.TotalSeconds < 60)
                {
                    canReturnGold = false;
                    return oldValue;
                }
                return nowUtc;
            });

            if (!canReturnGold)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you are on cooldown.");
                return;
            }

            if (guess == 0) //Make sure a guess is made
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you need to guess a number between 1 and 100!");
                return;
            }
            
            if (guess > 100 || guess < 1) //Make sure it's between 1 and 100
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you need to guess a number between 1 an 100!");
                return;
            }

            Random rnd = new Random(); //Get a random number
            int number = rnd.Next(1, 101);

            if (guess == number) //If guess is correct, give 50 gold
            {
                await Data.Data.SaveGold(Context.User.Id, 50, Context.Guild.Id, Context.User.Username);
                await Context.Channel.SendMessageAsync($"Congratulations {Context.User.Mention}! You have guessed the number correctly and earned 50 gold!");
                return;
            }
            else //if guess is incorrect, give 2 gold
            {
                await Data.Data.SaveGold(Context.User.Id, 2, Context.Guild.Id, Context.User.Username);
                await Context.Channel.SendMessageAsync($"Sorry {Context.User.Mention}, you guessed wrong! The number was {number}. You still gain 2 pity gold however.");
                return;
            }
        }
    }
}
