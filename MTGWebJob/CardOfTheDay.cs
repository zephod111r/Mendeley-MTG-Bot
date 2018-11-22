using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace MTGWebJob
{
    class CardOfTheDay : TableEntry
    {
        private static Random localRandom = new Random();
        private static Timer updateCotDTimer = null;
        private const string tableName = "cardoftheday";
        internal override string TableName { get { return tableName; } }
        private string _cardName = null;

        private static readonly string[] setImageFilter = {
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

        public CardOfTheDay() : base(Guid.NewGuid().ToString()) { }
        public string Set { get; set; }
        public string Card { get { return _cardName; } set { _cardName = value; RowKey = value.GetHashCode().ToString(); } }
        public string DisplayString { get; set; }
        public DateTime DateShown { get; set; }

        internal static void Display(Object o)
        {
            CardOfTheDay cotd = Get();

            try
            {
                if (cotd != null)
                {
                    Card current = MagicTheGathering.cardJson[cotd.Set].cards.Where(p => p.name == cotd.Card).First();
                    string message = "CotD: No one guessed correctly!<br>It was:<br>" + Output.displayCard(MagicTheGathering.cardJson[cotd.Set], current, 311, 223);
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

                    if (DateTime.Now.ToLocalTime().DayOfWeek != DayOfWeek.Saturday && DateTime.Now.ToLocalTime().DayOfWeek != DayOfWeek.Sunday)
                    {

                        bool cardOfTheDayFound = false;

                        List<SetData> setsToLookin = MagicTheGathering.cardJson.Values.Where(p => !setImageFilter.Contains(p.name, StringComparer.InvariantCultureIgnoreCase)).ToList();

                        while (!cardOfTheDayFound)
                        {
                            int setIndex = localRandom.Next() % setsToLookin.Count;

                            SetData set = setsToLookin.ElementAt(setIndex);
                            List<Card> rareMythic = set.cards.FindAll(p => p.rarity.ToUpper().Contains("RARE") && !CardOfTheDay.AlreadyUsed(p));

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

                            if (CardOfTheDay.AlreadyUsed(todisplay))
                            {
                                continue;
                            }

                            CardOfTheDay card = new CardOfTheDay();
                            card.PartitionKey = Program.Messenger.Room;
                            card.Card = todisplay.name;
                            card.Set = set.code;
                            card.DateShown = DateTime.Now;
                            card.DisplayString = "MTG Bot - Card of the day<br/><img src=" + ImageUtility.prepareCardImage(set, todisplay, true) + " />";
                            card.Save();

                            SettingString cardId = Setting.Get<SettingString>(tableName);
                            cardId.Value = todisplay.name;
                            cardId.Save();
                            cardOfTheDayFound = true;

                            Program.Messenger.SendMessage(card.DisplayString, MessageClient.MessageColour.Green);
                        }
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

                updateCotDTimer = new Timer(Display, null, (int)timeDiff.TotalMilliseconds, System.Threading.Timeout.Infinite);
            }
        }
        private static CardOfTheDay Get(string name)
        {
            if(name == null || name.Length == 0)
            {
                return null;
            }
            string RowKey = name.GetHashCode().ToString();
            string query = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Program.Messenger.Room);
            query = TableQuery.CombineFilters(query, TableOperators.And, TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, RowKey));
            return Program.AzureStorage.Populate<CardOfTheDay>(tableName, query);
        }
        static internal bool AlreadyUsed(Card todisplay)
        {
            CardOfTheDay nameAlreadyUsed = Get(todisplay);
            return nameAlreadyUsed != null;
        }
        static private CardOfTheDay Get(Card card)
        {
            if(card == null)
            {
                return null;
            }
            return Get(card.name);
        }
        static internal CardOfTheDay Get()
        {
            SettingString cardId = Setting.Get<SettingString>(tableName);
            return Get(cardId.Value);
        }
        static internal void Clear()
        {
            SettingString cardId = Setting.Get<SettingString>(tableName);
            cardId.Value = null;
            cardId.Save();
        }


        private static string RemovePunctuation(string value)
        {
            value = value.Replace(",", "");
            value = value.Replace("-", "");
            value = value.Replace("_", "");
            value = value.Replace(" ", "");
            value = value.Replace("\"", "");
            value = value.Replace("\\", "");
            value = value.Replace("/", "");
            value = value.Replace("'", "");
            value = value.Replace(";", "");
            value = value.Replace(":", "");
            value = value.Replace("!", "");
            value = value.Replace("#", "");
            value = value.Replace("?", "");
            value = value.Replace("`", "");
            return value;
        }

        internal static string New(string cardName, string requestingUser)
        {
            CardOfTheDay card = Get();

            switch (cardName.ToLower())
            {
                case "testing":
                    if (requestingUser.Contains("Phillip Hounslow"))
                    {
                        CardOfTheDay.Display(null);
                    }
                    return "";
                case "seasonend":
                    if (requestingUser.Contains("Phillip Hounslow"))
                    {
                        return Season.EndSeason();
                    }
                    return "";
                case "new":
                    {
                        if (card == null)
                        {
                            Player player = Player.GetPlayer(requestingUser);
                            if ((player.CotDRequest.Date != DateTime.Now.Date) && (player.LastCorrectGuess.Date != DateTime.Now.Date) &&
                                (DateTime.Now.Hour > 9 && DateTime.Now.Hour < 18 && DateTime.Now.DayOfWeek != DayOfWeek.Saturday && DateTime.Now.DayOfWeek != DayOfWeek.Sunday))
                            {
                                player.CotDRequest = DateTime.Now;
                                player.Save();
                                Display(null);
                            }
                        }
                        return "";
                    }

                case "score":
                    {
                        Dictionary<string, PlayerCounts> playerScores = CardOfTheDayGuess.GetPlayerCounts();
                        return Output.PrintScores(playerScores);
                    }

                case "show":
                    {
                        return card != null ? card.DisplayString : "";
                    }
                default:
                    {
                        if (card == null)
                            return requestingUser + " was Too Late!";

                        Season season = Season.Get();
                        string modifiedCardName = RemovePunctuation(cardName);
                        string cotdCardNameModified = RemovePunctuation(card.Card);
                        
                        CardOfTheDayGuess guess = new CardOfTheDayGuess();

                        guess.Card = card.Card;
                        guess.Guess = cardName;
                        guess.User = requestingUser;
                        guess.When = DateTime.Now;
                        guess.SeasonId = season.Id;

                        if (!modifiedCardName.Equals(cotdCardNameModified, StringComparison.CurrentCultureIgnoreCase))
                        {

                            guess.Save();

                            return requestingUser + " " + cardName + " (Incorrect)";
                        }

                        SetData set = MagicTheGathering.cardJson[card.Set];
                        Card cardObj = set.cards.Where(p => p.name == card.Card).First();
                        CardOfTheDay.Clear();

                        guess.WinningGuess = true;
                        guess.Save();

                        Player player = Player.GetPlayer(requestingUser);

                        player.LastCorrectGuess = DateTime.Now.ToLocalTime();
                        player.CotDRequest = DateTime.Now.ToLocalTime();
                        player.Save();


                        string ret = requestingUser + " Success    <br>" + Output.displayCard(set, cardObj, 311, 223);

                        Dictionary<string, PlayerCounts> playerScores = CardOfTheDayGuess.GetPlayerCounts();
                        ret += "<br>" + Output.PrintScores(playerScores);
                        
                        if (playerScores.Keys.Contains(guess.User) && playerScores[guess.User].Correct >= season.WinningCount)
                        {
                            ret += "<br>" + Season.EndSeason();
                        }

                        return ret;

                    }
            }
        }
    }
}
