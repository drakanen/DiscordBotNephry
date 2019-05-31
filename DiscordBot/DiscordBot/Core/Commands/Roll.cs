using System;
using System.Threading.Tasks;

using Discord.Commands;

namespace DiscordBot.Core.Commands
{
    public class Roll : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Rolls dice and displays the results
        /// </summary>
        /// <param name="arg">The size of the dice, amount of dice, and a bonus to add or subtract if applicable</param>
        [Command("roll"), Alias("Roll", "ROLL"), Summary("Rolls a N sided dice, bonus to total is optional")]
        public async Task RollDice(string arg)
        {
            //Make sure there is a roll amount
            if (arg.Length == 0)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention} please make sure you roll a value such as \"!roll 2d3+4\"");
                return;
            }
            //The amount of dice being rolled
            string AmountOfDice = "";

            //What sided of dice, such as 6 sided or 20 sided
            string SizeOfDice = "";

            //The place in the string to check
            int counter;

            //Get the amount of dice being rolled
            for (counter = 0; counter < arg.Length; counter++)
            {
                if (arg[counter] == 'd' || arg[counter] == 'D') //Get the number of dice before the D on the user input
                    break;
                AmountOfDice += arg[counter];
            }

            //Make sure it is formatted correctly
            if (counter == arg.Length - 1)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, please make sure the roll is formatted correctly. Proper format is : \"!roll 2d5\" or \"!roll 2d5+3\".");
                return;
            }

            //Holds if there is a bonus amount to add or take from the total
            bool bonus = false;
            bool add = false; //If add is true add the bonus, if false take away the bonus

            //Get the bonus if it exists and either add or minus it
            while (counter < arg.Length - 1)
            {
                ++counter;
                if (arg[counter] == '+')
                { bonus = true; add = true;  break; }
                else if (arg[counter] == '-')
                { bonus = true; add = false; break; }
                SizeOfDice += arg[counter];
            }
            int AmountOfDiceInt = 0; //Conversion of the string counterparts
            int SizeOfDiceInt = 0;

            try
            {   //Convert the string amounts into ints
                AmountOfDiceInt = int.Parse(AmountOfDice);
                SizeOfDiceInt = int.Parse(SizeOfDice);
            }
            catch (FormatException)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, please make sure the roll is formatted correctly. Proper format is : \"!roll 2d5\" or \"!roll 2d5+3\".");
                return;
            }

            //The total amount rolled
            int total = RollTheDice(AmountOfDiceInt, SizeOfDiceInt);
            int totalWithBonus = total; //Add the total rolled to the total with bonus
            int TotalBonusInt = 0; //Hold the bonus amount
            if (bonus)
            {
                try
                {   //Get the bonus to add or minus
                    TotalBonusInt = int.Parse(IncludeBonus(counter, arg));
                }
                catch (FormatException)
                {
                    await Context.Channel.SendMessageAsync($"{Context.User.Mention}, please make sure the roll is formatted correctly. Proper format is : \"!roll 2d5\" or \"!roll 2d5+3\".");
                    return;
                }

                if (add == true)
                { //Add the bonus to the total
                    totalWithBonus += TotalBonusInt;
                    await Context.Channel.SendMessageAsync($"{ Context.User.Mention}, Dice roll: {total} + {TotalBonusInt} = {totalWithBonus}");
                    return;
                }
                else
                {   //Minus the bonus from the total
                    totalWithBonus -= TotalBonusInt;
                    if (totalWithBonus < 1) //If the total rolled would be under 1, make it 1 instead
                        totalWithBonus = 1;
                    await Context.Channel.SendMessageAsync($"{Context.User.Mention}, Dice roll: {total} - {TotalBonusInt} = {totalWithBonus}");
                    return;
                }
            }
            await Context.Channel.SendMessageAsync($"{Context.User.Mention}, Dice roll: {total}");
        }

        /// <summary>
        /// Gets the bonus included in the roll
        /// </summary>
        /// <param name="i">The spot in the arg to look for the bonus</param>
        /// <param name="arg">The roll being done</param>
        /// <returns>The bonus included in the roll</returns>
        public string IncludeBonus(int i, string arg)
        {
            ++i; //Spot in the string to look at
            string TotalBonus = "";
            while (i < arg.Length)
            {
                TotalBonus += arg[i]; //Add the bonus
                ++i;
            }
            return TotalBonus;
        }
        
        /// <summary>
        /// Rolls the dice given
        /// </summary>
        /// <param name="amount">How many dice are being rolled</param>
        /// <param name="size">How many sides are on the die</param>
        /// <returns>The number rolled from the dice</returns>
        public int RollTheDice(int amount, int size)
        {
            Random rnd = new Random();
            int randomnum;
            int total = 0;
            for (int i = 0; i < amount; ++i)
            {
                randomnum = rnd.Next(1, size + 1); //Include the maximum size of the die
                total += randomnum;
            }
            return total;
        }
    }
}
