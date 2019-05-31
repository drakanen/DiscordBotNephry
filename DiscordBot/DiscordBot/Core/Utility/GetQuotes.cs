using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Core.Utility
{
    public class GetQuotes
    {
        /// <summary>
        /// Was used for the voting class. Gets the words between sets of quotation marks.
        /// Not currently used for anything now.
        /// </summary>
        /// <param name="info"></param>
        /// <returns>A string list of words that were between quotes. Each quote goes on a different element.</returns>
        public List<string> GetAllQuotes(string info)
        {
            int length = info.Length;
            bool openQuote = false;
            List<string> options = new List<string>();

            //Get voting options
            for (int i = 0; i < length; ++i)
            {
                if (info[i] == '"')
                {
                    if (openQuote == false)
                    {
                        openQuote = true;
                        info = info.Remove(0, i);
                        options.Add(GetBetweenQuotes(info));
                        i = 0;
                        length = info.Length;
                    }
                    else
                        openQuote = false;
                }
            }
            return options;
        }

        /// <summary>
        /// Gets the words between the quotes.
        /// </summary>
        /// <param name="info">The quotes to look between</param>
        /// <returns>The words between the quotes.</returns>
        public string GetBetweenQuotes(string info)
        {
            bool openQuote = false;
            bool closeQuote = false;
            string words = "";

            for (int i = 0; i < info.Length; ++i)
            {
                if (info[i] == '"' && openQuote == false)
                {
                    openQuote = true;
                }
                else if (info[i] != '"' && openQuote == true)
                {
                    words += info[i];
                }
                else if (info[i] == '"' && openQuote == true)
                {
                    closeQuote = true;
                }

                if (closeQuote == true)
                    return words;
            }
            return "";
        }
    }
}
