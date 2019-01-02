using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MTGWebJob
{
    class MagicTheGathering
    {
        const string regexPatternCard = @"{{(.+?)}}";
        const string regexPatternSet = @"\(\((.+)\)\)";

        const string regexCardOfTheDay = @"cotd|CotD|COTD";
        const string regexSearch = @"search|Search|SEARCH";
        const string regexRulings = @"rulings|Rulings|RULINGS";

        const string regexPatternRulings = @"^\/(?:" + regexRulings + ") (.+)";
        const string regexPatternCardOfTheDay = @"^\/(?:" + regexCardOfTheDay + ") (.+)";
        const string regexPatternSearch = @"^\/(?:" + regexSearch + ") " + HipchatMessenger.regexNamedParameters;

        private static Timer updateTimer = null;

        static internal Dictionary<string, SetData> cardJson = null;

        public static List<SetData> SetData { private get; set; }

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
            Program.Messenger.Handle(regexPatternRulings, doDisplayRulings);
            Program.Messenger.Handle(regexPatternCardOfTheDay, CardOfTheDay.New);
            Program.Messenger.Handle(regexPatternSearch, Search.New);
            RareOfTheDay.Display(null);
            CardOfTheDay.Display(null);
        }

        private string doDisplayRulings(string cardName, string userName)
        {
            return Output.GenerateCardData(cardName, SetData, true);
        }

        private static void UpdateAndLoadData(Object o)
        {
            //Get new json data on init (so I can just restart bot when new set comes out)
            using (WebClient WebClient = new WebClient())
            {
                Stream data = WebClient.OpenRead("https://mtgjson.com/json/AllSets.json.zip");
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
            Parallel.ForEach<SetData>(cardJson.Values, (set) => {
                Parallel.ForEach<Card>(set.cards, (card) => {
                    Console.Write("Processing: {0} {1}\n", set.name, card.name);
                    ImageUtility.uploadCardImage(set, card);
                    Console.Write("Completed: {0} {1}\n", set.name, card.name);
                });
            } );
            updateTimer = new Timer(UpdateAndLoadData, null, 24 * 60 * 60000, System.Threading.Timeout.Infinite);
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
            return Output.GenerateCardData(cardName, SetData);
        }

        private static string[] listChoices = { "printings", "colouridentity", "type", "types", "subtype", "subtypes" };
        private static string[] stringChoices = { "" };
        private static string[] intChoices = { "cmc", "manacost" };
    }
}
