using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Discord.Commands;
using Discord.WebSocket;

using DiscordBot.Core.Utility;

namespace DiscordBot.Core.Games
{
    public class Blackjack : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Private inner class that defines what a card is.
        /// </summary>
        private class Card
        {
            public string cardName;
            public int value;

            public Card()
            { }

            public Card(string name, int val)
            {
                cardName = name;
                value = val;
            }
        }

        //Holds the cooldown to use this command again
        private static readonly ConcurrentDictionary<long, DateTimeOffset> blackjackLastCall = new ConcurrentDictionary<long, DateTimeOffset>();
        private static List<ulong> currentlyInUse = new List<ulong>();
        private List<Card> playerCards = new List<Card>(); //Holds the cards the player has
        private List<Card> houseCards = new List<Card>(); //Holds the cards the AI/house has
        private string turn = "player"; //Holds whose turn it is
        private ISocketMessageChannel channel; //The channel to send messages to
        private SocketUser user; //The user to target
        private SocketGuild guild; //The guild the user is in
        private int bet; //The amount the user betted
        private bool houseStand = false; //Holds if the house has standed
        private bool playerStand = false; //Holds if the player has standed
        private bool gameOver = false; //Holds if the game is over
        private bool busted = false; //Holds if someone busted
        private GetUserInput input = new GetUserInput(); //Gets user input
        private Stopwatch timeout = new Stopwatch(); //Timeout timer for the game
        private readonly int[] numberOfEachCard = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; //Holds how many of each card exist

        //All of the cards that can be pulled
        private readonly Card[] cardList = new Card[] { new Card("Empty", 0), new Card("Ace", 11), new Card("1", 1), new Card("2", 2), new Card("3", 3) , new Card("4", 4), new Card("5", 5),
            new Card("6", 6), new Card("7", 7), new Card("8", 8), new Card("9", 9), new Card("10", 10), new Card ("Jack", 10), new Card ("Queen", 10), new Card("King", 10)};


        /// <summary>
        /// Creates a game of blackjack
        /// </summary>
        /// <param name="playerBet">The amount of gold the player has betted.</param>
        [Command("blackjack"), Summary("Starts a simple game of blackjack")]
        public async Task StartGame(int playerBet = 0)
        {
            if (currentlyInUse.Contains(Context.Guild.Id))
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention} a blackjack game is already being played, please wait for it to finish.");
                return;
            }
            
            ulong id = Context.User.Id;

            DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
            bool canReturnBlackjack = true;

            //Cool down timer
            blackjackLastCall.AddOrUpdate((long)id, nowUtc, (key, oldValue) =>
            {
                TimeSpan elapsed = nowUtc - oldValue;
                if (elapsed.TotalSeconds < 0)
                {
                    canReturnBlackjack = false;
                    return oldValue;
                }
                return nowUtc;
            });

            if (!canReturnBlackjack)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you are on cooldown.");
                return;
            }

            

            if (playerBet == 0)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention} you must include a bet amount to play blackjack!");
                return;
            }

            if (Data.Data.GetGold(Context.User.Id, Context.Guild.Id) < bet)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention} you do not have that much gold to bet!");
                return;
            }

            //Only one game can be played in the guild at a time
            currentlyInUse.Add(Context.Guild.Id);

            guild = Context.Guild; //Get necessary information
            channel = Context.Channel;
            user = Context.User;
            bet = playerBet;

            houseCards.Add(GetCard()); //Get 2 cards for the house
            houseCards.Add(GetCard());
            playerCards.Add(GetCard()); //Get 2 cards for the player
            playerCards.Add(GetCard());
            await DisplayStartingHouseHand(); //Display one house card
            CheckHouseStartingHand(); //If amount is already at or over 17, house stands
            await DisplayCard(); //Display player's cards
            await MakeChoice(); //Let them make their choices in the game

            //Allow another game to be played in the guild
            currentlyInUse.Remove(guild.Id);
        }

        /// <summary>
        /// Hosts the game, getting user input and calling the other methods as appropriate
        /// </summary>
        private async Task MakeChoice()
        {
            await input.MainAsync(guild, user);
            //Get input

            //While the player is not standing and game has not ended
            while (playerStand == false && gameOver == false)
            {
                input.answer = ""; //Reset the answer in the class that gets input
                timeout.Restart(); //Restart the timer after each answer
                string answer = ""; //Reset the answer that's held here

                //Check for an answer until a choice is picked
                while (answer != "hit" && answer != "stand" && answer != "double down" &&
                    answer != "surrender")
                {
                    answer = input.answer; //Get their answer

                    //If 20 seconds pass without a response, end the game
                    if (timeout.ElapsedMilliseconds > 20000)
                    {
                        await Context.Channel.SendMessageAsync($"{Context.User.Mention} has not responded to the game so it has ended. Bets have not been returned.");
                        await Data.Data.SaveGoldMinus(user.Id, bet, guild.Id, user.Username);
                        return;
                    }
                }
                if (answer == "hit") //If the player wants to hit
                {
                    await Hit("player"); //Hit them
                    if (playerStand == false)
                        await DisplayCard(); //If player has not standed
                }
                else if (answer == "stand")
                    Stand("player"); //Stand
                else if (answer == "double down")
                    DoubleDown(); //Hit once then stand
                else if (answer == "surrender") //Stop playing, take half of bet away
                    await Surrender();
                
            }

            while (houseStand == false && gameOver == false)
                await Hit("ai"); //While the house has not standed and game is not over, hit the house

            if (gameOver == false) //If game is not over from someone going over, decide the winner
                await DecideWinner();
        }

        /// <summary>
        /// Creates a card from the card list
        /// </summary>
        /// <returns>A card with its name and value</returns>
        private Card GetCard()
        {
            Random ran = new Random();
            bool notAlreadyTooMany; //Make sure there's less than 5 of each type of card
            int cardNumber; //the card number gained from random number
            do //Get a card number
            {
                notAlreadyTooMany = true;
                cardNumber = ran.Next(1, 14); //Get the card number

                //Make sure there's not too many cards of that type already
                for (int i = 1; i < 14; ++i)
                {
                    if (numberOfEachCard[cardNumber] > 4)
                    {
                        notAlreadyTooMany = false;
                        break;
                    }
                }

            } while (notAlreadyTooMany == false); //While there's not more than 4 of the card
            
            //Create a card
            Card card = cardList[cardNumber + 1];
            ++numberOfEachCard[cardNumber]; //Increase the amount of cards there are
            return card;
        }

        /// <summary>
        /// Display the cards held by the player or house
        /// </summary>
        private async Task DisplayCard()
        {
            string totalcards = ""; //All of the cards
            if (turn == "ai") //On the house's turn
            {
                foreach (Card card in houseCards) //Get the house's cards
                {
                    totalcards += card.cardName + ", ";
                }
                totalcards = totalcards.Remove(totalcards.Length - 2); //Remove the extra ", "
                await channel.SendMessageAsync($"House cards\n`{totalcards}`"); //Display the cards
            }
            else if (turn == "player") //On the player's turn
            {
                foreach (Card card in playerCards) //Get the player's cards
                {
                    totalcards += card.cardName + ", ";
                }
                totalcards = totalcards.Remove(totalcards.Length - 2); //Remove the extra ", "
                await channel.SendMessageAsync($"Your cards\n`{totalcards}`"); //Display the cards
            }
        }

        /// <summary>
        /// Display one of the starting house cards
        /// </summary>
        private async Task DisplayStartingHouseHand()
        {
            await channel.SendMessageAsync($"House card\n`{houseCards[0].cardName}`"); //Display the house's first card
        }

        /// <summary>
        /// Check house starting hand for a value is equal to or over 17
        /// Stand if it is
        /// </summary>
        private void CheckHouseStartingHand()
        {
            int total = 0;
            foreach (Card card in houseCards)
            {
                total += card.value; //Get the value of the house's first two cards
            }
            if (total >= 17)
                houseStand = true; //Stand if at or over 17
        }

        /// <summary>
        /// Add a card to the player or house's hand and just the value.
        /// If it's a bust, report it as so and end the game.
        /// </summary>
        /// <param name="hitter">Who is getting another card, either the player or house.</param>
        private async Task Hit(string hitter)
        {
            int total = 0; //Holds the total worth of the cards
            if (hitter == "ai") //if the house is hitting
            {
                houseCards.Add(GetCard()); //Get a card
                foreach (Card card in houseCards)
                {
                    total += card.value; //Add up the value of the cards
                }

                if (total > 21) //If over 21, check for an ace
                {
                    foreach (Card card in houseCards)
                    {
                        if (card.cardName == "Ace" && total > 21)
                        {
                            total -= card.value; //Remove the ace's value
                            card.value = 1; //Change the value to 1
                            total += 1; //Add 1 back to the total
                        }
                    }

                    if (total > 21) //Busted
                    {   //Give the player their bet's worth
                        await Data.Data.SaveGold(user.Id, bet, guild.Id, user.Username);
                        await DisplayCard(); //Display the house's hand
                        await channel.SendMessageAsync($"{user.Mention} the dealer has busted with a total of {total}! " +
                            $"You now have {Data.Data.GetGold(user.Id, guild.Id)} gold.");
                        gameOver = true; //End the game
                        busted = true;
                        return;
                    }
                    else if (total >= 17) //Stand
                        Stand("ai");
                }
                else if (total >= 17) //Stand
                {
                    Stand("ai");
                }
            }
            else if (hitter == "player") //If player is hitting
            {
                playerCards.Add(GetCard()); //Add a card
                foreach (Card card in playerCards)
                {
                    total += card.value; //Get value of cards
                }
                
                if (total > 21) //if total is above 21, check for an ace
                {
                    //Change any aces from 11 to 1 if they are in the deck
                    foreach (Card card in playerCards)
                    {
                        if (card.cardName == "Ace" && total > 21)
                        {
                            total -= card.value; //Remove the value of the ace
                            card.value = 1; //Change it to 1
                            total += 1; //Add 1 back to the total
                        }
                    }

                    if (total > 21) //Busted
                    {   //Lose bet'd amount of gold
                        await Data.Data.SaveGoldMinus(user.Id, bet, guild.Id, user.Username);
                        await channel.SendMessageAsync($"{user.Mention} you have busted with a total of {total}!" +
                            $"\nYou now have {Data.Data.GetGold(user.Id, guild.Id)} gold.");
                        gameOver = true; //End the game
                        busted = true;
                        return;
                    }
                    else if (total == 20) //If player has 20 exact, stand
                    {
                        Stand("player");
                        await channel.SendMessageAsync($"{user.Mention} you have a total of 20. Standing now.");
                    }
                }
                else if (total == 20) //If player has 20 exact, stand
                {
                    Stand("player");
                    await channel.SendMessageAsync($"{user.Mention} you have a total of 20. Standing now.");
                }
            }
        }

        /// <summary>
        /// Marks the player or house as standing.
        /// </summary>
        /// <param name="stander">Who is standing, either the player or house.</param>
        private void Stand(string stander)
        {
            if (stander == "ai") //Stand the house
                houseStand = true;
            else if (stander == "player")
            {
                playerStand = true; //Stand the player, make the house go next
                turn = "ai";
            }
        }
        
        /// <summary>
        /// Doubles the pot, hits the player and stands.
        /// </summary>
        private void DoubleDown()
        {
                bet *= 2; //Double bet
#pragma warning disable CS4014 //Being buggy, wants me to add await but then it doesn't want me to add await
                Hit("player"); //Hit the player
                Stand("player"); //Stand
        }

        /// <summary>
        /// Lose half your bet to end the game.
        /// </summary>
        private async Task Surrender()
        {
            double changeGold = bet * 0.50; //Decrease gambled gold by half

            await Data.Data.SaveGoldMinus(user.Id, (int)Math.Round(changeGold, MidpointRounding.AwayFromZero), guild.Id, user.Username); //Take away half the bet
            
            await channel.SendMessageAsync($"{user.Mention} you have surrendered half your gold. " +
                $"You now have {Data.Data.GetGold(user.Id, guild.Id)} gold."); //Display their new value
            gameOver = true; //End the game
        }

        /// <summary>
        /// Compares the values of the player and house to decide who wins.
        /// Displays the winner and ends the game.
        /// </summary>
        private async Task DecideWinner()
        {
            if (busted == true) //if someone busted game has already ended
                return;

            int houseTotal = 0;
            int playerTotal = 0; //Holds the totals
            await channel.SendMessageAsync($"Game is over. Deciding winner now.\n");
            await DisplayCard(); //Display the house's cards
            turn = "player";
            await DisplayCard(); //Display the player's cards
            foreach (Card card in houseCards)
            {
                houseTotal += card.value; //Get the house's total
            }

            foreach (Card card in playerCards)
            {
                playerTotal += card.value; //Get the player's total
            }

            //If House won
            if (houseTotal > playerTotal)
            {
                Data.Data.SaveGoldMinus(user.Id, bet, guild.Id, user.Username);
                await channel.SendMessageAsync($"The house has {houseTotal}. Player has {playerTotal}. The house wins!" +
                    $" You now have {Data.Data.GetGold(user.Id, guild.Id)} gold.");
            }
            //If player won
            else if (houseTotal < playerTotal)
            {
                Data.Data.SaveGold(user.Id, bet, guild.Id, user.Username);
                await channel.SendMessageAsync($"The house has {houseTotal}. Player has {playerTotal}. The player wins!\n" +
                    $"You now have {Data.Data.GetGold(user.Id, guild.Id)} gold.");
            }
            //If player and house tied, win goes to house
            else if (houseTotal == playerTotal)
            {
                Data.Data.SaveGoldMinus(user.Id, bet, guild.Id, user.Username);
                await channel.SendMessageAsync($"The house has {houseTotal}. Player has {playerTotal}. The house wins on tied games!" +
                   $" You now have { Data.Data.GetGold(user.Id, guild.Id)} gold.");
            }
        }
    }
}