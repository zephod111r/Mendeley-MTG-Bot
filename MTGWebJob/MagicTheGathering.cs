using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using HipchatApiV2.Enums;
using Newtonsoft.Json;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;


namespace HipchatMTGBot
{
    class MagicTheGathering
    {
        private const string PlayerDataTable = "cotdscore";
        private const string CotDTableName = "cotdcards";
        private const string RotDTableName = "rotd";

        private static double decayRateK = Math.Log(0.916666667) / (30.14 * 24 * 60 * 60 * 1000);

        public static string[] setImageFilter = {
            "Collector's Edition",
            "International Collector's Edition",
            "15th Anniversary",
            "Anthologies",
            "Arena League",
            "Asia Pacific Land Program",
            "Celebration",
            "Champs and States",
            "Coldsnap Theme Decks",
            "Deckmasters",
            "Duels of the Planeswalkers",
            "European Land Program",
            "Friday Night Magic",
            "Gateway",
            "Grand Prix",
            "Guru",
            "Happy Holidays",
            "Introductory Two-Player Set",
            "Judge Gift Program",
            "Launch Parties",
            "Legend Membership",
            "Magic Game Day",
            "Magic Player Rewards",
            "Media Inserts",
            "Multiverse Gift Box",
            "Portal Demo Game",
            "Prerelease Events",
            "Pro Tour",
            "Release Events",
            "Rivals Quick Start Set",
            "Summer of Magic",
            "Super Series",
            "Two-Headed Giant Tournament",
            "Wizards of the Coast Online Store",
            "Wizards Play Network",
            "World Magic Cup Qualifiers",
            "Worlds"
        };

        const string regexPatternCard = @"{{(.+?)}}";
        const string regexPatternSet = @"\(\((.+)\)\)";

        const string regexCardOfTheDay = @"cotd|CotD|COTD";
        const string regexSearch = @"search|Search|SEARCH";
        const string regexRulings = @"rulings|Rulings|RULINGS";

        const string regexPatternRulings = @"^\/(?:" + regexRulings + ") (.+)";
        const string regexPatternCardOfTheDay = @"^\/(?:" + regexCardOfTheDay + ") (.+)";
        const string regexPatternSearch = @"^\/(?:" + regexSearch + ") " + HipchatMessenger.regexNamedParameters;

        private static Timer updateTimer = null;
        private static Timer updateRotDTimer = null;
        private static Timer updateCotDTimer = null;

        private static List<string> codUsedCards = new List<string>();

        private static Random localRandom = new Random();

        static Dictionary<string, SetData> cardJson = null;

        public static List<SetData> SetData { private get; set; }

        private static CotD CotD
        {
            get; set;
        }

        public MagicTheGathering()
        {
            if (!Directory.Exists("cards"))
            {
                Directory.CreateDirectory("cards");
            }
            if (!Directory.Exists("cropped"))
            {
                Directory.CreateDirectory("cropped");
            }

            //load jsonData and load list of cards currently mentioned without sending a billion notifications
            UpdateAndLoadData(null);
            Program.Messenger.Handle(regexPatternSet, setSetToUse);
            Program.Messenger.Handle(regexPatternCard, getCard);
            Program.Messenger.Handle(regexPatternCardOfTheDay, doCardOfTheDay);
            Program.Messenger.Handle(regexPatternRulings, doDisplayRulings);
            Program.Messenger.Handle(regexPatternSearch, doSearch);
            DisplayRareOfTheDay(null);
            DisplayCardOfTheDay(null);
        }

        private string doDisplayRulings(string cardName, string userName)
        {
            return GenerateCardData(cardName, SetData, true);
        }

        private static void UpdateAndLoadData(Object o)
        {
            //Get new json data on init (so I can just restart bot when new set comes out)
            using (WebClient WebClient = new WebClient())
            {
                Stream data = WebClient.OpenRead("http://mtgjson.com/json/AllSets-x.json.zip");
                using (ZipArchive archive = new ZipArchive(data))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        Stream entryStream = entry.Open();
                        using (var r = new StreamReader(entryStream))
                        {
                            string json = r.ReadToEnd();
                            Dictionary<string, SetData> cards = JsonConvert.DeserializeObject<Dictionary<string, SetData>>(json);
                            cardJson = cards;
                        }
                    }
                }
            }

            LoadData();
            updateTimer = new Timer(UpdateAndLoadData, null, 24 * 60 * 60000, System.Threading.Timeout.Infinite);
        }

        private static void LoadData()
        {
            using (var r = new StreamReader("AllSets-x.json"))
            {
                string json = r.ReadToEnd();
                Dictionary<string, SetData> cards = JsonConvert.DeserializeObject<Dictionary<string, SetData>>(json);
                cardJson = cards;
            }
        }

        private static string displayCard(SetData set, Card card, int height, int width)
        {
            var cardImg = "<img src=\"" + ImageUtility.prepareCardImage(set, card) + "\" height=\"" + height + "\" width=\"" + width + "\">";
            return string.Format(@"<a href=""http://gatherer.wizards.com/Pages/Card/Details.aspx?name={0}"">{1}<br/>{2}</a>",
                    HttpUtility.UrlEncode(card.name), card.name, cardImg);
        }

        private static int GetCardPucaPoints(Card card)
        {
            using (WebClient WebClient = new WebClient())
            {
                WebClient.DownloadFile("https://www.mkmapi.eu/ws/v2.0/products/find?search=" + HttpUtility.UrlEncode(card.name) + "&exact=true", HttpUtility.UrlEncode(card.name) + ".json");
            }
            return 0;
        }

        private static void DisplayRareOfTheDay(Object o)
        {
            if (updateRotDTimer != null)
            {
                updateRotDTimer = null;

                bool cardOfTheDayFound = false;

                while (!cardOfTheDayFound)
                {
                    int setIndex = localRandom.Next() % cardJson.Count;

                    SetData set = cardJson.Values.ElementAt(setIndex);
                    List<Card> rareMythic = set.cards.FindAll(p => p.rarity.ToUpper().Contains("RARE"));
                    rareMythic.RemoveAll(c => codUsedCards.Contains(c.name.ToUpper()));

                    if (rareMythic.Count == 0)
                    {
                        continue;
                    }

                    int index = localRandom.Next() % rareMythic.Count;
                    Card todisplay = rareMythic.ElementAt(index);
                    
                    string query = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Program.Messenger.Room);
                    query = TableQuery.CombineFilters(query, TableOperators.And, TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, todisplay.name));
                    RotDCard nameAlreadyUsed = Program.AzureStorage.Populate<RotDCard>(RotDTableName, query);
                    if(nameAlreadyUsed != null)
                    {
                        continue;
                    }

                    codUsedCards.Add(todisplay.name.ToUpper());

                    RotDCard card = new RotDCard();
                    card.PartitionKey = Program.Messenger.Room;
                    card.RowKey = todisplay.name;
                    card.DateShown = DateTime.Now;
                    Program.AzureStorage.UploadTableData(card, RotDTableName);

                    List<SetData> sets = new List<SetData>();
                    sets.Add(set);
                    var cardData = "MTG Bot - Card of the day<br/>" + GenerateCardData(todisplay.name, sets);
                    Program.Messenger.SendMessage(cardData, MessageClient.MessageColour.Yellow);
                    cardOfTheDayFound = true;
                }
            }

            var targetTime = DateTime.Now;
            if (targetTime.Hour >= 10 && targetTime.Hour < 15)
            {
                targetTime = targetTime.AddHours(14 - targetTime.Hour);
            }
            else if (targetTime.Hour >= 15)
            {
                targetTime = targetTime.AddHours(9 - targetTime.Hour);
                targetTime = targetTime.AddDays(1);
                if (targetTime.DayOfWeek == DayOfWeek.Saturday)
                {
                    targetTime = targetTime.AddDays(2);
                }
                if (targetTime.DayOfWeek == DayOfWeek.Sunday)
                {
                    targetTime = targetTime.AddDays(1);
                }
            }
            else if (targetTime.Hour < 10)
            {
                targetTime = targetTime.AddHours(9 - targetTime.Hour);
            }

            targetTime = targetTime.AddMinutes(59 - targetTime.Minute);
            targetTime = targetTime.AddSeconds(59 - targetTime.Second);
            targetTime = targetTime.AddMilliseconds(1000 - targetTime.Millisecond);

            var timeDiff = targetTime - DateTime.Now;
            updateRotDTimer = new Timer(DisplayRareOfTheDay, null, (int)timeDiff.TotalMilliseconds, System.Threading.Timeout.Infinite);
        }
        
        private static void DisplayCardOfTheDay(Object o)
        {
            try
            {
                if (CotD != null)
                {
                    string message = "CotD: No one guessed correctly!<br>It was:<br>" + displayCard(cardJson.Values.Where(p => p.cards.Contains(CotD.card)).First(), CotD.card, 311, 223);
                    Program.Messenger.SendMessage(message, MessageClient.MessageColour.Yellow);
                }
            }
            catch (Exception)
            { }

            try
            {
                if (updateCotDTimer != null)
                {
                    updateCotDTimer = null;

                    bool cardOfTheDayFound = false;

                    List<SetData> setsToLookin = cardJson.Values.Where(p => !setImageFilter.Contains(p.name, StringComparer.InvariantCultureIgnoreCase)).ToList();

                    while (!cardOfTheDayFound)
                    {
                        int setIndex = localRandom.Next() % setsToLookin.Count;

                        SetData set = setsToLookin.ElementAt(setIndex);
                        List<Card> rareMythic = set.cards.FindAll(p => p.rarity.ToUpper().Contains("RARE"));
                        rareMythic.RemoveAll(c => codUsedCards.Contains(c.name.ToUpper()));

                        if (rareMythic.Count == 0)
                        {
                            continue;
                        }

                        int index = localRandom.Next() % rareMythic.Count;
                        Card todisplay = rareMythic.ElementAt(index);

                        string layout = todisplay.layout;
                        if (!string.Equals(layout, "normal", StringComparison.CurrentCultureIgnoreCase))
                        {
                            continue;
                        }

                        string query = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Program.Messenger.Room);
                        query = TableQuery.CombineFilters(query, TableOperators.And, TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, todisplay.name));
                        CotDCard nameAlreadyUsed = Program.AzureStorage.Populate<CotDCard>(CotDTableName, query);
                        if (nameAlreadyUsed != null)
                        {
                            continue;
                        }

                        CotDCard card = new CotDCard();
                        card.PartitionKey = Program.Messenger.Room;
                        card.RowKey = todisplay.name;
                        card.DateShown = DateTime.Now;
                        card.GuessingPlayer = null;
                        card.DateGuessed = DateTime.Now;
                        Program.AzureStorage.UploadTableData(card, CotDTableName);

                        cardOfTheDayFound = true;
                        CotD = new CotD();
                        CotD.display = "MTG Bot - Card of the day<br/><img src=" + ImageUtility.prepareCardImage(set, todisplay, true) + " />";
                        CotD.card = todisplay;
                        CotD.set = set;

                        codUsedCards.Add(todisplay.name.ToUpper());

                        Program.Messenger.SendMessage(CotD.display, MessageClient.MessageColour.Green);
                    }
                }
            }
            catch (Exception) { }
            finally
            {
                var targetTime = DateTime.Now;
                if (targetTime.Hour >= 10 && targetTime.Hour < 15)
                {
                    targetTime = targetTime.AddHours(14 - targetTime.Hour);
                }
                else if (targetTime.Hour >= 15)
                {
                    targetTime = targetTime.AddHours(9 - targetTime.Hour);
                    targetTime = targetTime.AddDays(1);
                    if (targetTime.DayOfWeek == DayOfWeek.Saturday)
                    {
                        targetTime = targetTime.AddDays(2);
                    }
                    if (targetTime.DayOfWeek == DayOfWeek.Sunday)
                    {
                        targetTime = targetTime.AddDays(1);
                    }
                }
                else if (targetTime.Hour < 10)
                {
                    targetTime = targetTime.AddHours(9 - targetTime.Hour);
                }

                targetTime = targetTime.AddMinutes(59 - targetTime.Minute);
                targetTime = targetTime.AddMinutes(30);
                targetTime = targetTime.AddSeconds(59 - targetTime.Second);
                targetTime = targetTime.AddMilliseconds(1000 - targetTime.Millisecond);

                var timeDiff = targetTime - DateTime.Now;

                updateCotDTimer = new Timer(DisplayCardOfTheDay, null, (int)timeDiff.TotalMilliseconds, System.Threading.Timeout.Infinite);
            }
        }

        private static string GenerateCardData(string cardData, List<SetData> setDataToUse, bool showRulings=false)
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
            
            if (setDataToUse == null || setDataToUse.Where(s => s.cards.Where(c=> c.name.ToUpper() == cardName.ToUpper()).Count() != 0).Count() == 0)
            {
                setDataToUse = cardJson.Values.Where(q => q.cards.Any(p => p.name.ToLower() == cardName.ToLower())).ToList();
            }
            
            var latestCardSet = setDataToUse.OrderBy(p=>p.releaseDate).LastOrDefault();
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
                if(showRulings == true)
                {
                    html += prettyPrintRules(card);
                }
            }
            else
            {
                html = "Exact match not found.  Best Matching card:<br />";
                if (cards == null)
                    cards = FuzzyMatch.Match(cardJson, cardName, numResults);
                card = cards[0].card;
                html += displayCard(cardJson.Values.Where(p=>p.cards.Contains(card)).First(), card, 311, 223);
                longForm = true;
            }

            if (longForm)
            {
                if (cards == null)
                    cards = FuzzyMatch.Match(cardJson, cardName, numResults);
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
                    html += displayCard(cardJson.Values.Where(p => p.cards.Contains(c.card)).First(), c.card, 105, 75);
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
            
            return "Card Not Recognized. Did you mean?..." + FuzzyMatch.BestMatch(cardJson, cardName);
        }

        private static string getHtmlText(Card card)
        {
            if (card.text == null)
            {
                return "";
            }

            string widthAlignedText = widthAlign(card.text);

            widthAlignedText = MTGSymbols.convertToHtmlSymbols(widthAlignedText);

            return String.Format("<td>{0}<br>{1}<br/><br/>{2}<br/><br/>{3}<br/></td>", MTGSymbols.convertToHtmlSymbols(card.manaCost), card.type, card.rarity, widthAlignedText);
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

            if(card.rulings != null && card.rulings.Count != 0)
            {
                output = "<table>";
                foreach(Ruling rule in card.rulings.OrderByDescending(p=>p.date))
                {
                    DateTime date;
                    DateTime.TryParse(rule.date, out date);
                    output += "<tr><td><ul><li> </li></ul></td><td>" + date.ToLongDateString() + "</td><td></td><td>" + widthAlign(rule.text, 100) + "</td></tr>";
                }
                output += "</table>";
            }

            return output;
        }

        public static Dictionary<string, string> GetHelp(ref Dictionary < string, string> items)
        {
            items.Add(@"{{<card name>}}", "Look up a specific card name");
            items.Add(@"{{<partial card name>:<maxtodisplay>:<maxcolumns>}}", @"Look up a (partial) card name and return up to Min(21, maxtodisplay) items across Min(10, columns) columns.");
            items.Add(@"((<set name>))", @"Cards searched for on the same line will look up in the specific set.  This uses exact matching on the set name.");
            items.Add(@"/" + regexSearch + " type|colour|cmc|manacost|subtype|name|text|colouridentity|printings=<value>", @"Search for a card matching all the search parameters given.  Note you can only search for one of each type of param ");
            items.Add(@"/" + regexCardOfTheDay + @" show|score|<your guess>", @"Display score, show the current CotD (if any!) or take a guess at the current CotD!");
            items.Add(@"/" + regexRulings + @" <card name>", @"Display the card and corresponding Rulings.");
            return items;
        }

        public static string setSetToUse(string setName, string requestingUser)
        {
            setName = setName.Replace("((", "");
            setName = setName.Replace("))", "");
            SetData = cardJson.Values.Where(p=>(p.name == setName) || (p.code == setName)).ToList();
            if (SetData != null && SetData.Count != 0)
            {
                return "<b>Attempting to display Cards from " + SetData.First().name + ":</b>";
            }
            return "<b>'Set' Filter Failed to Find " + setName + "</b>";
        }

        private static string getCard(string cardName, string requestingUser)
        {
            cardName = cardName.Replace("{{", "");
            cardName = cardName.Replace("}}", "");
            return GenerateCardData(cardName, SetData);
        }

        private static string RemovePunctuation(string value)
        {
            value = value.Replace(",", "");
            value = value.Replace("-", "");
            value = value.Replace("_", "");
            value = value.Replace(" ", "");
            value = value.Replace("\"", "");
            value = value.Replace("'", "");
            value = value.Replace(";", "");
            value = value.Replace(":", "");
            value = value.Replace("!", "");
            return value;
        }

        private static string doCardOfTheDay(string cardName, string requestingUser)
        {
            if(cardName.Equals("new", StringComparison.CurrentCultureIgnoreCase) && CotD == null)
            {
                string query = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Program.Messenger.Room);
                query = TableQuery.CombineFilters(query, TableOperators.And, TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, requestingUser));
                Player player = Program.AzureStorage.Populate<Player>(PlayerDataTable, query);
                if ((player == null || 
                    player.CotDRequest.Date != DateTime.Now.Date) && 
                    (DateTime.Now.Hour > 9 && DateTime.Now.Hour < 18 && DateTime.Now.DayOfWeek != DayOfWeek.Saturday && DateTime.Now.DayOfWeek != DayOfWeek.Sunday))
                {
                    if(player == null)
                    {
                        player = new Player();
                        player.PartitionKey = Program.Messenger.Room;
                        player.RowKey = requestingUser;
                        player.Version = 0;
                        UpdatePlayerVersion(player);
                    }

                    player.CotDRequest = DateTime.Now;
                    Program.AzureStorage.UploadTableData(player, PlayerDataTable);
                    DisplayCardOfTheDay(null);
                }
                return "";
            }
            else if (cardName.Equals("score", StringComparison.CurrentCultureIgnoreCase))
            {
                List<Player> players = new List<Player>();
                Program.AzureStorage.Populate<Player>(out players, PlayerDataTable, Program.Messenger.Room);

                string ret = "Current Player Scores are:<br><table>";

                // Decay by 1 Each Month.
                System.Globalization.CultureInfo myCI = new System.Globalization.CultureInfo("en-GB", false);

                System.Globalization.CultureInfo myCIclone = (System.Globalization.CultureInfo)myCI.Clone();
                myCIclone.NumberFormat.NumberDecimalDigits = 2;
                
                int i = 0;
                foreach (var player in players.OrderByDescending(p => p.RankScore))
                {
                    Player output = EvaluateRank(player);

                    ++i;
                    ret += "<tr><td>" + i.ToString() + ".) </td><td>" + output.RowKey + "</td><td></td><td>" + output.CotDScore.ToString() + "</td><td>    (" + output.RankScore.ToString("N2") + ")</td><td>    [Total Score: " + (output.TotalScore + output.CotDScore).ToString("N2") + "]</td></tr>";
                }
                ret += "</table>";
                return ret;
            }

            if (CotD == null)
                return requestingUser + " was Too Late!";

            if(cardName.Equals("show", StringComparison.CurrentCultureIgnoreCase))
            {
                return CotD.display;
            }

            string modifiedCardName = RemovePunctuation(cardName);
            string cotdCardNameModified = RemovePunctuation(CotD.card.name);

            if (modifiedCardName.Equals(cotdCardNameModified, StringComparison.CurrentCultureIgnoreCase))
            {
                string query = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Program.Messenger.Room);
                query = TableQuery.CombineFilters(query, TableOperators.And, TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, requestingUser));
                Player player = Program.AzureStorage.Populate<Player>(PlayerDataTable, query);

                if(player == null)
                {
                    player = new Player();
                    player.PartitionKey = Program.Messenger.Room;
                    player.RowKey = requestingUser;
                    player.Version = 0;
                    player.CotDScore = 0;
                    UpdatePlayerVersion(player);
                }

                Player output = EvaluateRank(player);
                output.LastCorrectGuess = DateTime.Now;
                output.CotDScore += 1;
                output.RankScore += 1;
                output.CotDRequest = DateTime.Now;
                Program.AzureStorage.UploadTableData(output, PlayerDataTable);
                
                string query2 = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Program.Messenger.Room);
                query2 = TableQuery.CombineFilters(query2, TableOperators.And, TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, CotD.card.name));
                CotDCard card = Program.AzureStorage.Populate<CotDCard>(CotDTableName, query2);

                if (card != null)
                {
                    card.DateGuessed = DateTime.Now;
                    card.GuessingPlayer = requestingUser;
                    Program.AzureStorage.UploadTableData(card, CotDTableName);
                }

                string ret = requestingUser + " Success<br>" + displayCard(CotD.set, CotD.card, 311, 223);
                CotD = null;

                return ret;
            }

            return requestingUser + " " + cardName + " (Incorrect)";
        }

        private static Player EvaluateRank(Player player)
        {
            UpdatePlayerVersion(player);

            TimeSpan offset = DateTime.Now - player.LastCorrectGuess;
            player.RankScore = player.RankScore * Math.Pow(Math.E, offset.TotalMilliseconds * decayRateK);
            Program.AzureStorage.UploadTableData(player, PlayerDataTable);

            return player;
        }

        private static void UpdatePlayerVersion(Player player)
        {
            if (player.Version < 2)
            {
                player.Version = 2;
                player.RankScore = player.CotDScore;
                player.LastCorrectGuess = DateTime.Now;
                player.CotDRequest = DateTime.Now;
            }
            if (player.Version < 3)
            {
                player.Version = 3;
                player.TotalScore = player.CotDScore;
            }
            if(player.Version < 4)
            {
                player.Version = 4;
                player.TotalScore = 0;
                player.CotDScore = 0;
                player.LastCorrectGuess = DateTime.Now;
                player.CotDRequest = DateTime.Now;
            }
            if(player.Version < 5)
            {
                player.Version = 5;
                player.RankScore = 0;
            }
            if (player.Version < 6)
            {
                player.Version = 6;
                player.TotalScore += player.CotDScore;
                player.RankScore = 0;
                player.CotDScore = 0;
            }
            if (player.Version < 7)
            {
                player.Version = 7;
                player.TotalScore += player.CotDScore;
                player.RankScore = 0;
                player.CotDScore = 0;
            }
            if (player.Version < 8)
            {
                player.Version = 8;
                player.TotalScore = 0;
                player.RankScore = 0;
                player.CotDScore = 0;
            }
        }

        private static string[] listChoices = { "printings", "colouridentity", "type", "types", "subtype", "subtypes" };
        private static string[] stringChoices = { "" };
        private static string[] intChoices = { "cmc", "manacost" };

        private static bool doMatch(Card card, Dictionary<string, string> search)
        {
            foreach(KeyValuePair<string, string> pair in search)
            {
                if (pair.Key == "name")
                {
                    if(card.name == null || !card.name.ToLower().Contains(pair.Value))
                    {
                        return false;
                    }
                }
                else if (pair.Key == "manacost")
                {
                    if(card.manaCost==null ||!card.manaCost.ToLower().Contains(pair.Value))
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
                    foreach(string value in values)
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
                    foreach(string type in types)
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
                        if(!types.Contains(type))
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

        private static string doSearch(Dictionary<string,string> search, string requestingUser)
        {
            Dictionary<Card, SetData> cardsFound = new Dictionary<Card, SetData>();
            foreach(SetData set in cardJson.Values)
            {
                foreach(Card card in set.cards)
                {
                    if(doMatch(card, search))
                    {
                        cardsFound.Add(card, set);
                    }
                }
            }
            string returnVal = "<table>";
            int count = 0;
            List<string> cardsAlreadyDisplayed = new List<string>();

            foreach (KeyValuePair<Card, SetData> cardPair in cardsFound.OrderByDescending(p=>p.Value.releaseDate))
            {
                Card card = cardPair.Key;
                SetData set = cardPair.Value;

                if(cardsAlreadyDisplayed.Contains(card.name))
                {
                    continue;
                }

                cardsAlreadyDisplayed.Add(card.name);

                if (count%3 == 0)
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


                if(count >= 9)
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
