using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTGWebJob
{
    class Search
    {
        internal static string doSearch(Dictionary<string, string> search, string requestingUser)
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

            Dictionary<Card, SetData> cardsFound = new Dictionary<Card, SetData>();
            foreach (SetData set in cardJson.Values)
            {
                foreach (Card card in set.cards)
                {
                    if (Search.doMatch(card, search))
                    {
                        cardsFound.Add(card, set);
                    }
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
                returnVal += displayCard(set, card, 210, 150);
                returnVal += "</td>";

                if (search.ContainsKey("display") && search["display"] == "full")
                {
                    returnVal += getHtmlText(card);
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

        private static bool doMatch(Card card, Dictionary<string, string> search)
        {
            foreach (KeyValuePair<string, string> pair in search)
            {
                if (pair.Key == "maxresults")
                {
                    continue;
                }
                else if (pair.Key == "name")
                {
                    if (card.name == null || !card.name.ToLower().Contains(pair.Value))
                    {
                        return false;
                    }
                }
                else if (pair.Key == "manacost")
                {
                    if (card.manaCost == null || !card.manaCost.ToLower().Contains(pair.Value))
                    {
                        return false;
                    }
                }
                else if (pair.Key == "cmc")
                {
                    float cmc = -1.0f;
                    float.TryParse(pair.Value, out cmc);
                    if (cmc != card.cmc)
                    {
                        return false;
                    }
                }
                else if (pair.Key == "power")
                {
                    if (card.power == null || !card.power.ToLower().Equals(pair.Value))
                    {
                        return false;
                    }
                }
                else if (pair.Key == "toughness")
                {
                    if (card.toughness == null || !card.toughness.ToLower().Equals(pair.Value))
                    {
                        return false;
                    }
                }
                else if (pair.Key == "type" || pair.Key == "types")
                {
                    if (card.type == null || !card.type.ToLower().Contains(pair.Value))
                    {
                        return false;
                    }
                }
                else if (pair.Key == "printing" || pair.Key == "printings")
                {
                    string[] values = pair.Value.Split(',');
                    foreach (string value in values)
                    {
                        if (card.printings == null || !card.printings.Contains(value, StringComparer.CurrentCultureIgnoreCase))
                        {
                            return false;
                        }
                    }
                }
                else if (pair.Key == "subtype" || pair.Key == "subtypes")
                {
                    string[] types = pair.Value.Split(',');
                    foreach (string type in types)
                    {
                        if (card.subtypes == null || !card.subtypes.Contains(type, StringComparer.CurrentCultureIgnoreCase))
                        {
                            return false;
                        }
                    }
                }
                else if (pair.Key == "types")
                {
                    if (card.types == null)
                    {
                        return false;
                    }

                    string[] types = pair.Value.Split(',');
                    foreach (string type in types)
                    {
                        if (!card.types.Contains(type, StringComparer.CurrentCultureIgnoreCase))
                        {
                            return false;
                        }
                    }
                }
                else if (pair.Key == "colouridentity")
                {
                    if (card.colorIdentity == null)
                    {
                        return false;
                    }

                    string[] types = pair.Value.Split(',');

                    foreach (string type in types)
                    {
                        if (!card.colorIdentity.Contains(type, StringComparer.CurrentCultureIgnoreCase))
                        {
                            return false;
                        }
                    }

                    string[] possibles = "w,r,g,b,u".Split(',');
                    List<string> values = new List<string>();
                    foreach (string type in possibles)
                    {
                        if (!types.Contains(type))
                        {
                            values.Add(type);
                        }
                    }

                    foreach (string type in values)
                    {
                        if (card.colorIdentity.Contains(type, StringComparer.CurrentCultureIgnoreCase))
                        {
                            return false;
                        }
                    }
                }
                else if (pair.Key == "text")
                {
                    if (card.text == null || !card.text.ToLower().Contains(pair.Value))
                    {
                        return false;
                    }
                }
                else if (pair.Key == "colour" || pair.Key == "colours")
                {
                    if (card.colors == null)
                    {
                        if (pair.Value == "none")
                            continue;
                        return false;
                    }

                    string[] values = pair.Value.Split(',');
                    foreach (string value in values)
                    {
                        if (!card.colors.Contains(value, StringComparer.CurrentCultureIgnoreCase))
                        {
                            return false;
                        }
                    }
                }
                else if (pair.Key == "rarity")
                {
                    if (card.rarity == null || card.rarity.ToLower() != pair.Value)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
