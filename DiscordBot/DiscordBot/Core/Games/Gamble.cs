using System;
using System.Threading.Tasks;

using Discord.Commands;
using System.Collections.Concurrent;

namespace DiscordBot.Core.Games
{
    public class Gamble : ModuleBase<SocketCommandContext>
    {
        private static readonly ConcurrentDictionary<long, DateTimeOffset> gambleLastCall = new ConcurrentDictionary<long, DateTimeOffset>();

        /// <summary>
        /// Gambles a betted amount of money. 2/7 chance to win, 1/7 to tie, 4/7 to lose money.
        /// </summary>
        /// <param name="gambleAmount">The amount of gold being gambled.</param>
        [Command("gamble"), Summary("Gambles a betted amount of gold")]
        public async Task StartGamble(string gambleAmount = "") //string because you can bet "all" instead of a number
        {
            ulong id = Context.User.Id;//Information for the cooldown test
            DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
            bool canReturnGold = true;

            //Cool down timer
            gambleLastCall.AddOrUpdate((long)id, nowUtc, (key, oldValue) =>
            {
                TimeSpan elapsed = nowUtc - oldValue;
                if (elapsed.TotalSeconds < 60)
                {
                    canReturnGold = false;
                    return oldValue;
                }
                return nowUtc;
            });

            if (canReturnGold == false)
            {
                await Context.Channel.SendMessageAsync("You are on cooldown.");
                return;
            }

            if (gambleAmount == "")
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention} you have to gamble an amount of gold!");
                return;
            }

            int amount = 0;
            try
            {
                amount = int.Parse(gambleAmount); //If a number was betted
            }
            catch (Exception) //if they went all in
            {
                if (gambleAmount == "all" || gambleAmount == "allin")
                    amount = Data.Data.GetGold(Context.User.Id, Context.Guild.Id);
                else
                {
                    await Context.Channel.SendMessageAsync($"{Context.User.Mention}, please enter either a number or \"all\" to go all in.");
                    return;
                }
            }

            //Get the amount of gold the user has
            int amountOfGold = Data.Data.GetGold(Context.User.Id, Context.Guild.Id);

            //If the user wants to gamble more gold than they have, say they can't and return
            if (amount > amountOfGold)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you do not have that much gold to gamble!");
                return;
            }

            Random rnd = new Random(); //Get a random number
            int number = rnd.Next(1, 8);
            double changeGold;

            switch (number) //Case-switch for deciding how gamble ends
            {
                case 1: //Double gambled amount
                    amountOfGold += amount;
                    await Context.Channel.SendMessageAsync($"Congratulations {Context.User.Mention}, you rolled a 1 and have doubled your gambled gold! You now have {amountOfGold} gold!");
                    await Data.Data.SaveGold(Context.User.Id, amount, Context.Guild.Id, null);
                    break;
                case 2:
                    changeGold = amount * 0.50; //Increase gambled gold by half
                    amountOfGold += (int)Math.Round(changeGold, MidpointRounding.AwayFromZero);
                    await Context.Channel.SendMessageAsync($"Congratulations {Context.User.Mention}, you rolled a 2 and have increased your gambled gold by half! You now have {amountOfGold} gold!");
                    await Data.Data.SaveGold(Context.User.Id, (int)Math.Round(changeGold, MidpointRounding.AwayFromZero), Context.Guild.Id, null);
                    break;
                case 3:
                    await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you rolled a 3 so your gold has not changed!"); //Nothing happens
                    return;
                case 4:
                    changeGold = amount * 0.25; //Decrease gambled gold by a quarter
                    amountOfGold -= (int)Math.Round(changeGold, MidpointRounding.AwayFromZero);
                    await Context.Channel.SendMessageAsync($"Uh oh! {Context.User.Mention}, you rolled a 4 and have lost some of your gambled gold! You now have {amountOfGold} gold!");
                    await Data.Data.SaveGoldMinus(Context.User.Id, (int)Math.Round(changeGold, MidpointRounding.AwayFromZero), Context.Guild.Id, null);
                    break;
                case 5:
                    changeGold = amount * 0.50; //Decrease gambled gold by half
                    amountOfGold -= (int)Math.Round(changeGold, MidpointRounding.AwayFromZero);
                    await Context.Channel.SendMessageAsync($"Uh oh! {Context.User.Mention}, you rolled a 5 and have lost around half of your gambled gold! You now have {amountOfGold} gold!");
                    await Data.Data.SaveGoldMinus(Context.User.Id, (int)Math.Round(changeGold, MidpointRounding.AwayFromZero), Context.Guild.Id, null);
                    break;
                case 6:
                    changeGold = amount * 0.75; //Decrease gambled gold by three quarters
                    amountOfGold -= (int)Math.Round(changeGold, MidpointRounding.AwayFromZero);
                    await Context.Channel.SendMessageAsync($"Uh oh! {Context.User.Mention}, you rolled a 6 and have lost most of your gambled gold! You now have {amountOfGold} gold!");
                    await Data.Data.SaveGoldMinus(Context.User.Id, (int)Math.Round(changeGold, MidpointRounding.AwayFromZero), Context.Guild.Id, null);
                    break;
                case 7:
                    amountOfGold -= amount; //Lose all gambled gold
                    await Context.Channel.SendMessageAsync($"Uh oh! {Context.User.Mention}, you rolled a 7 and have lost all of your gambled gold! You now have {amountOfGold} gold!");
                    await Data.Data.SaveGoldMinus(Context.User.Id, amount, Context.Guild.Id, null);
                    break;
            }
        }
    }
}
