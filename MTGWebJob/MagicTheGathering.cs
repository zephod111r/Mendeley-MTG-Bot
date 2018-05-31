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
using System.Threading.Tasks;

namespace MTGWebJob
{
    class MagicTheGathering
    {
        private const string RotDTableName = "rotd";
        
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

        private static List<string> codUsedCards = new List<string>();


        static internal Dictionary<string, SetData> cardJson = null;

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
            Program.Messenger.Handle(regexPatternSearch, Search.doSearch);
            RotD.Display(null);
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
            /*
            Parallel.ForEach<SetData>(cardJson.Values, (set) => {
                Parallel.ForEach<Card>(set.cards, (card) => {
                    Console.Write("Processing: {0} {1}\n", set.name, card.name);
                    ImageUtility.uploadCardImage(set, card);
                    Console.Write("Completed: {0} {1}\n", set.name, card.name);
                });
            } );
            */
            updateTimer = new Timer(UpdateAndLoadData, null, 24 * 60 * 60000, System.Threading.Timeout.Infinite);
        }


        private static int GetCardPucaPoints(Card card)
        {
            using (WebClient WebClient = new WebClient())
            {
                WebClient.DownloadFile("https://www.mkmapi.eu/ws/v2.0/products/find?search=" + HttpUtility.UrlEncode(card.name) + "&exact=true", HttpUtility.UrlEncode(card.name) + ".json");
            }
            return 0;
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
            switch(cardName.ToLower())
            {
                case "seasonend":
                    if(requestingUser == "Phillip Hounslow")
                    {
                        return Season.EndSeason();
                    }
                    return "";
                case "new":
                    {
                        if(CotD == null)
                        {
                            Player player = Player.GetPlayer(requestingUser);
                            if ((player.CotDRequest.Date != DateTime.Now.ToLocalTime().Date) &&
                                (DateTime.Now.ToLocalTime().Hour > 9 && DateTime.Now.ToLocalTime().Hour < 18 && DateTime.Now.ToLocalTime().DayOfWeek != DayOfWeek.Saturday && DateTime.Now.ToLocalTime().DayOfWeek != DayOfWeek.Sunday))
                            {
                                player.CotDRequest = DateTime.Now.ToLocalTime();
                                player.Save();
                                DisplayCardOfTheDay(null);
                            }
                        }
                        return "";
                    }

                case "score":
                    {
                        Dictionary<string, PlayerCounts> playerScores = CotdGuess.GetPlayerCounts();
                        return CotdGuess.PrintScores(playerScores);
                    }

                case "show":
                    {
                        return CotD != null ? CotD.display : "";
                    }
                default:
                    {
                        if (CotD == null)
                            return requestingUser + " was Too Late!";
                    
                        string modifiedCardName = RemovePunctuation(cardName);
                        string cotdCardNameModified = RemovePunctuation(CotD.card.name);

                        SettingString currentSeasonId = Setting.Get<SettingString>(Season.SettingName);
                        CotdGuess guess = new CotdGuess();

                        guess.Card = CotD.card.name;
                        guess.Guess = cardName;
                        guess.User = requestingUser;
                        guess.When = DateTime.Now.ToLocalTime();
                        guess.SeasonId = currentSeasonId.Value;


                        if (modifiedCardName.Equals(cotdCardNameModified, StringComparison.CurrentCultureIgnoreCase))
                        {
                            guess.WinningGuess = true;
                            guess.Save();

                            Player player = Player.GetPlayer(requestingUser);

                            player.LastCorrectGuess = DateTime.Now.ToLocalTime();
                            player.CotDRequest = DateTime.Now.ToLocalTime();
                            player.Save();

                            UsedCotD card = UsedCotD.Get(CotD.card);

                            if (card != null)
                            {
                                card.DateGuessed = guess.When;
                                card.GuessingPlayer = guess.User;
                                card.Save();
                            }

                            string ret = requestingUser + " Success<br>" + displayCard(CotD.set, CotD.card, 311, 223);
                            CotD = null;

                            Dictionary<string, PlayerCounts> playerScores = CotdGuess.GetPlayerCounts();
                            ret += "<br>" + CotdGuess.PrintScores(playerScores);

                            Season season = Season.Get(guess.SeasonId);
                            if(playerScores[guess.User].Count > season.WinningCount)
                            {
                                ret += "<br>" + Season.EndSeason();
                            }

                            return ret;
                        }

                        guess.Save();

                        return requestingUser + " " + cardName + " (Incorrect)";
                    }
            }
        }
        
        private static string[] listChoices = { "printings", "colouridentity", "type", "types", "subtype", "subtypes" };
        private static string[] stringChoices = { "" };
        private static string[] intChoices = { "cmc", "manacost" };

        



    }
}
