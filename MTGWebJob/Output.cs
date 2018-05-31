using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MTGWebJob
{
    class Output
    {

        internal static string displayCard(SetData set, Card card, int height, int width)
        {
            var cardImg = "<img src=\"" + ImageUtility.prepareCardImage(set, card) + "\" height=\"" + height + "\" width=\"" + width + "\">";
            return string.Format(@"<a href=""http://gatherer.wizards.com/Pages/Card/Details.aspx?name={0}"">{1}<br/>{2}</a>",
                    HttpUtility.UrlEncode(card.name), card.name, cardImg);
        }

        internal static string GenerateCardData(string cardData, List<SetData> setDataToUse, bool showRulings = false)
        {
            string cardName = "";
            int numResults = 3;
            int numColumns = 3;
            int test = 0;
            int column = 0;
            Boolean longForm = false;

            string[] cd = cardData.Split(new char[] { ':' });

            if (cd.Length > 0)
                cardName = cd[0];

            if (cd.Length > 1)
            {
                if (int.TryParse(cd[1], out test))
                {
                    if (test > 21)
                    {
                        test = 21;
                    }
                    if (test > 0)
                    {
                        longForm = true;
                        numResults = test;
                    }
                }
            }

            if (cd.Length > 2)
            {
                if (int.TryParse(cd[2], out test))
                {
                    if (test > 10)
                    {
                        test = 10;
                    }
                    else if (test > 0 && test < 3)
                    {
                        test = 3;
                    }
                    if (test > 0)
                    {
                        longForm = true;
                        numColumns = test;
                    }
                }
            }

            if (setDataToUse == null || setDataToUse.Where(s => s.cards.Where(c => c.name.ToUpper() == cardName.ToUpper()).Count() != 0).Count() == 0)
            {
                setDataToUse = MagicTheGathering.cardJson.Values.Where(q => q.cards.Any(p => p.name.ToLower() == cardName.ToLower())).ToList();
            }

            var latestCardSet = setDataToUse.OrderBy(p => p.releaseDate).LastOrDefault();
            Card card = null;

            string html;
            CardResult[] cards = null;

            if (latestCardSet != null)
            {
                card = latestCardSet.cards.Last(c => c.name.ToUpper() == cardName.ToUpper());
                html = "<table><tr><td>";
                html += displayCard(latestCardSet, card, 311, 223);
                html += "</td>";
                if (card.text != null)
                {
                    html += getHtmlText(card);
                }

                html += "</tr></table>";
                if (showRulings == true)
                {
                    html += prettyPrintRules(card);
                }
            }
            else
            {
                html = "Exact match not found.  Best Matching card:<br />";
                if (cards == null)
                    cards = FuzzyMatch.Match(MagicTheGathering.cardJson, cardName, numResults);
                card = cards[0].card;
                html += displayCard(MagicTheGathering.cardJson.Values.Where(p => p.cards.Contains(card)).First(), card, 311, 223);
                longForm = true;
            }

            if (longForm)
            {
                if (cards == null)
                    cards = FuzzyMatch.Match(MagicTheGathering.cardJson, cardName, numResults);
                cards = cards.Skip(1).ToArray();
                if (numColumns > cards.Length)
                {
                    numColumns = cards.Length;
                }
                html += string.Format("<br/><table><tr>", numResults - 1);
                column = 0;
                foreach (CardResult c in cards)
                {
                    if (column == 0)
                    {
                        html += "<tr>";
                    }
                    html += "<td>";
                    html += displayCard(MagicTheGathering.cardJson.Values.Where(p => p.cards.Contains(c.card)).First(), c.card, 105, 75);
                    html += "</td>";
                    column += 1;
                    column %= numColumns;
                    if (column == 0)
                        html += "</tr>";
                }
                if (column != 0)
                    html += "</tr>";
                html += "</table>";
            }

            if (card != null)
            {
                return html;
            }

            return "Card Not Recognized. Did you mean?..." + FuzzyMatch.BestMatch(MagicTheGathering.cardJson, cardName);
        }

        internal static string getHtmlText(Card card)
        {
            if (card.text == null)
            {
                return "";
            }

            string widthAlignedText = widthAlign(card.text);

            widthAlignedText = MagicTheGatheringSymbols.convertToHtmlSymbols(widthAlignedText);

            return string.Format("<td>{0}<br>{1}<br/><br/>{2}<br/><br/>{3}<br/></td>", MagicTheGatheringSymbols.convertToHtmlSymbols(card.manaCost), card.type, card.rarity, widthAlignedText);
        }

        private static string widthAlign(string cardText, int width = 50)
        {
            cardText = cardText.Replace(".", ". ");
            cardText = cardText.Replace(". )", ".) ");
            cardText = cardText.Replace(". \"", ".\"");
            string[] text = cardText.Split(' ');

            int nextWord = 0;
            string widthAlignedText = "";
            while (nextWord < text.Length)
            {
                string nextLine = "<br/>";
                while (nextWord < text.Length && nextLine.Length < width)
                {
                    nextLine += text[nextWord];
                    if (text[nextWord].EndsWith(".") || text[nextWord].EndsWith(".)"))
                    {
                        ++nextWord;
                        break;
                    }
                    else
                    {
                        nextLine += " ";
                    }
                    ++nextWord;
                }
                widthAlignedText += nextLine;
            }
            return widthAlignedText;
        }

        private static string prettyPrintRules(Card card)
        {
            string output = "";

            if (card.rulings != null && card.rulings.Count != 0)
            {
                output = "<table>";
                foreach (Ruling rule in card.rulings.OrderByDescending(p => p.date))
                {
                    DateTime date;
                    DateTime.TryParse(rule.date, out date);
                    output += "<tr><td><ul><li> </li></ul></td><td>" + date.ToLongDateString() + "</td><td></td><td>" + widthAlign(rule.text, 100) + "</td></tr>";
                }
                output += "</table>";
            }

            return output;
        }
        internal static string PrintScores(Dictionary<string, PlayerCounts> playerScores)
        {
            string ret = "Current Player Scores are:<br><table>";

            int i = 0;
            foreach (var player in playerScores.OrderByDescending(p => p.Value.Count))
            {
                ++i;
                ret += "<tr><td>" + i.ToString() + ".) </td><td>" + player.Key + "</td><td></td><td>" + player.Value.Correct.ToString() + "</td><td>    (" + player.Value.Count + ")</td></tr>";
            }
            ret += "</table>";
            return ret;
        }
        internal static string ShowSearchResults(Dictionary<Card, SetData> cardsFound, Dictionary<string, string> search, string requestingUser)
        {
            int maxResults = 9;
            if (search.Keys.Contains("maxresults"))
            {
                int.TryParse(search["maxresults"], out maxResults);

                if (maxResults < 9)
                {
                    maxResults = 9;
                }

                if (maxResults > 9 && maxResults % 3 != 0)
                {
                    maxResults -= (maxResults % 3);
                }

                if (maxResults > 66)
                {
                    maxResults = 66;
                }
            }
            string returnVal = "<table>";
            int count = 0;
            List<string> cardsAlreadyDisplayed = new List<string>();

            foreach (KeyValuePair<Card, SetData> cardPair in cardsFound.OrderByDescending(p => p.Value.releaseDate))
            {
                Card card = cardPair.Key;
                SetData set = cardPair.Value;

                if (cardsAlreadyDisplayed.Contains(card.name))
                {
                    continue;
                }

                cardsAlreadyDisplayed.Add(card.name);

                if (count % 3 == 0)
                {
                    returnVal += "<tr>";
                }
                returnVal += "<td>";
                returnVal += Output.displayCard(set, card, 210, 150);
                returnVal += "</td>";

                if (search.ContainsKey("display") && search["display"] == "full")
                {
                    returnVal += Output.getHtmlText(card);
                }
                ++count;


                if (count >= maxResults)
                {
                    returnVal += "</tr>";
                    break;
                }
                else if (count % 3 == 0)
                {
                    returnVal += "</tr>";
                }
            }
            returnVal += "</table>";
            return returnVal;
        }
    }
}
