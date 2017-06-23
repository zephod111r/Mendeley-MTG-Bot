﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HipchatApiV2;
using HipchatApiV2.Requests;
using HipchatApiV2.Responses;
using HipchatApiV2.Enums;
using Newtonsoft.Json;
using System.Web;
using System.Drawing;
using TheArtOfDev.HtmlRenderer;
using TheArtOfDev.HtmlRenderer.WinForms;


namespace HipchatMTGBot
{
    class MagicTheGathering
    {
        private const string TableName = "mtgtable";
        static private Dictionary<string, string> symbolReplacement = new Dictionary<string, string>()
        {
            { "{C}",  "<img alt='{C}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=C&type=symbol' width='15px' height='15px' />" },
            { "{∞}",  "<img alt='{∞}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=Infinity&type=symbol' width='15px' height='15px' />" },
            { "{½}",  "<img alt='{½}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=Half&type=symbol' width='15px' height='15px' />" },
            { "{S}",  "<img alt='{S}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=Snow&type=symbol' width='15px' height='15px' />" },
            { "{E}",  "<img alt='{E}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=E&type=symbol' width='15px' height='15px' />" },
            { "{0}",  "<img alt='{0}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=0&type=symbol' width='15px' height='15px' />" },
            { "{1}",  "<img alt='{1}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=1&type=symbol' width='15px' height='15px' />" },
            { "{2}",  "<img alt='{2}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=2&type=symbol' width='15px' height='15px' />" },
            { "{3}",  "<img alt='{3}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=3&type=symbol' width='15px' height='15px' />" },
            { "{4}",  "<img alt='{4}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=4&type=symbol' width='15px' height='15px' />" },
            { "{5}",  "<img alt='{5}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=5&type=symbol' width='15px' height='15px' />" },
            { "{6}",  "<img alt='{6}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=6&type=symbol' width='15px' height='15px' />" },
            { "{7}",  "<img alt='{7}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=7&type=symbol' width='15px' height='15px' />" },
            { "{8}",  "<img alt='{8}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=8&type=symbol' width='15px' height='15px' />" },
            { "{9}",  "<img alt='{9}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=9&type=symbol' width='15px' height='15px' />" },
            { "{10}", "<img alt='{10}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=10&type=symbol' width='15px' height='15px' />" },
            { "{11}", "<img alt='{11}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=11&type=symbol' width='15px' height='15px' />" },
            { "{12}", "<img alt='{12}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=12&type=symbol' width='15px' height='15px' />" },
            { "{13}", "<img alt='{13}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=13&type=symbol' width='15px' height='15px' />" },
            { "{14}", "<img alt='{14}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=14&type=symbol' width='15px' height='15px' />" },
            { "{15}", "<img alt='{15}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=15&type=symbol' width='15px' height='15px' />" },
            { "{16}", "<img alt='{16}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=16&type=symbol' width='15px' height='15px' />" },
            { "{100}", "<img alt='{100}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=100&type=symbol' width='15px' height='15px' />" },
            { "{X}",  "<img alt='{X}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=X&type=symbol' width='15px' height='15px' />" },
            { "{W}",  "<img alt='{W}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=W&type=symbol' width='15px' height='15px' />" },
            { "{U}",  "<img alt='{U}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=U&type=symbol' width='15px' height='15px' />" },
            { "{B}",  "<img alt='{B}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=B&type=symbol' width='15px' height='15px' />" },
            { "{R}",  "<img alt='{R}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=R&type=symbol' width='15px' height='15px' />" },
            { "{hr}",  "<img alt='{hr}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=HalfR&type=symbol' width='8px' height='15px' />" },
            { "{G}",  "<img alt='{G}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=G&type=symbol' width='15px' height='15px' />" },
            { "{R/G}", "<img alt='{R/G}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=RG&type=symbol' width='15px' height='15px' />" },
            { "{W/U}", "<img alt='{W/U}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=WU&type=symbol' width='15px' height='15px' />" },
            { "{U/R}", "<img alt='{U/R}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=UR&type=symbol' width='15px' height='15px' />" },
            { "{U/B}", "<img alt='{U/B}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=UB&type=symbol' width='15px' height='15px' />" },
            { "{B/R}", "<img alt='{B/R}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=BR&type=symbol' width='15px' height='15px' />" },
            { "{B/G}", "<img alt='{B/G}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=BG&type=symbol' width='15px' height='15px' />" },
            { "{G/U}", "<img alt='{G/U}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=GU&type=symbol' width='15px' height='15px' />" },
            { "{G/W}", "<img alt='{G/W}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=GW&type=symbol' width='15px' height='15px' />" },
            { "{R/W}", "<img alt='{R/W}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=RW&type=symbol' width='15px' height='15px' />" },
            { "{W/B}", "<img alt='{W/B}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=WB&type=symbol' width='15px' height='15px' />" },
            { "{2/W}", "<img alt='{2/W}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=2W&type=symbol' width='15px' height='15px' />" },
            { "{2/U}", "<img alt='{2/U}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=2U&type=symbol' width='15px' height='15px' />" },
            { "{2/B}", "<img alt='{2/B}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=2B&type=symbol' width='15px' height='15px' />" },
            { "{2/R}", "<img alt='{2/R}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=2R&type=symbol' width='15px' height='15px' />" },
            { "{2/G}", "<img alt='{2/G}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=2G&type=symbol' width='15px' height='15px' />" },
            { "{G/P}", "<img alt='{G/P}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=GP&type=symbol' width='15px' height='15px' />" },
            { "{R/P}", "<img alt='{R/P}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=RP&type=symbol' width='15px' height='15px' />" },
            { "{B/P}", "<img alt='{B/P}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=BP&type=symbol' width='15px' height='15px' />" },
            { "{W/P}", "<img alt='{W/P}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=WP&type=symbol' width='15px' height='15px' />" },
            { "{U/P}", "<img alt='{U/P}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=UP&type=symbol' width='15px' height='15px' />" },
            { "{T}",  "<img alt='{T}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=tap&type=symbol' width='15px' height='15px' />" },
            { "{Q}",  "<img alt='{Q}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=untap&type=symbol' width='15px' height='15px' />" }
        };

        const string regexPatternCard = @"{{(.+?)}}";
        const string regexPatternSet = @"\(\((.+)\)\)";
        const string regexPatternManaOrTapSymbol = @"{[^{}]+}";

        const string regexCardOfTheDay = @"cotd|CotD|COTD";
        const string regexSearch = @"search|Search|SEARCH";
        const string regexRulings = @"rulings|Rulings|RULINGS";

        const string regexPatternRulings = @"\/(?:" + regexRulings + ") (.+)";
        const string regexPatternCardOfTheDay = @"\/(?:" + regexCardOfTheDay + ") (.+)";
        const string regexPatternSearch = @"\/(?:" + regexSearch + ") " + HipchatMessenger.regexNamedParameters;

        private static Timer updateTimer = null;
        private static Timer updateRotDTimer = null;
        private static Timer updateCotDTimer = null;

        private static List<string> codUsedCards = new List<string>();

        private static Random localRandom = new Random();

        static Dictionary<string, SetData> cardJson = null;
        
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
                if (File.Exists("AllSets-x.json"))
                {
                    if (File.GetCreationTime("AllSets-x.json") < DateTime.Now.AddDays(-1.0))
                    {
                        File.Delete("AllSets-x.json");
                        WebClient.DownloadFile("http://mtgjson.com/json/AllSets-x.json.zip", "AllSets.json.zip");
                        using (ZipArchive archive = ZipFile.OpenRead("AllSets.json.zip"))
                        {
                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                entry.ExtractToFile(Path.Combine("", entry.FullName));
                            }
                        }
                    }
                }
                else
                {
                    WebClient.DownloadFile("http://mtgjson.com/json/AllSets-x.json.zip", "AllSets.json.zip");
                    using (ZipArchive archive = ZipFile.OpenRead("AllSets.json.zip"))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            entry.ExtractToFile(Path.Combine("", entry.FullName));
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

            foreach(SetData set in cardJson.Values)
            {
                foreach(Card card in set.cards)
                {
                    prepareCardImage(set, card);
                }
            }
        }

        private static string displayCard(SetData set, Card card, int height, int width)
        {
            var cardImg = "<img src=\"" + prepareCardImage(set, card) + "\" height=\"" + height + "\" width=\"" + width + "\">";
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
        
        private static string uploadCardImage(SetData set, Card card, Image src)
        {
            string setName = HttpUtility.UrlEncode(set.name).Replace("%", "");

            if (!Directory.Exists("cards/" + setName))
            {
                Directory.CreateDirectory("cards/" + setName);
            }

            string name = HttpUtility.UrlEncode(card.name) + ".jpeg";
            src.Save("cards/" + setName + "/" + name);
            return Program.AzureStorage.Upload(name, setName, "cards");
        }
        
        private static string uploadCroppedCardImage(SetData set, Card card, Image src)
        {
            string cardName = HttpUtility.UrlEncode(card.name.GetHashCode().ToString() + ".jpeg");
            string setName = HttpUtility.UrlEncode(set.name.GetHashCode().ToString());
            Bitmap target = new Bitmap(170, 130);
            if (src != null)
            {
                using (Graphics g = Graphics.FromImage(target))
                {
                    g.DrawImage(src, new Rectangle(0, 0, target.Width, target.Height),
                                     new Rectangle(28, 40, target.Width, target.Height),
                                     GraphicsUnit.Pixel);
                }
                target.Save("cropped/" + setName + "/" + cardName);
            }
            return Program.AzureStorage.Upload(setName + cardName, "cropped", "");
        }

        private static string prepareCardImage(SetData set, Card card, bool showCropped = false)
        {
            string setName = HttpUtility.UrlEncode(set.name).Replace("%", "");
            if (!Directory.Exists("cards/" + setName))
            {
                Directory.CreateDirectory("cards/" + setName);
            }

            string name = HttpUtility.UrlEncode(card.name) + ".jpeg";
            Image src = null;
            string url = "";
            try
            {
                src = Image.FromFile("cards/" +setName + "/" + name);
                url = Program.AzureStorage.IsBlobPresent(name, setName);
            }
            catch (Exception)
            {

            }

            if (src == null || url == "")
            {
                try
                {
                    string imageSrc = @"<!DOCTYPE html><html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml""><head><meta charset=""utf-8"" /><title></title></head><body style=""margin:0px;padding:0px;border-radius:5px;background-color:transparent;""><img src=http://gatherer.wizards.com/Handlers/Image.ashx?multiverseid=" + card.multiverseid + @"&amp;type=card style=""margin:0px;padding:0px;border-radius:5px;background-color:transparent;"" width=223 height=311 /></body></html>";
                    src = TheArtOfDev.HtmlRenderer.WinForms.HtmlRender.RenderToImage(imageSrc);
                    url = uploadCardImage(set, card, src);
                }
                catch (Exception) { }
            }

            if (showCropped == true)
            {
                try
                {
                    string cardName = HttpUtility.UrlEncode(card.name.GetHashCode().ToString() + ".jpeg");
                    url = Program.AzureStorage.IsBlobPresent(cardName, "cropped");
                    if (url.Equals("") || File.Exists("cards/" + setName + "/" + HttpUtility.UrlEncode(card.name) + ".jpeg") || Program.AzureStorage.Download(setName, name, "cards"))
                    { 
                        src = Image.FromFile("cards/" + setName + "/" + HttpUtility.UrlEncode(card.name) + ".jpeg");
                        url = uploadCroppedCardImage(set, card, src);
                    }
                }
                catch (Exception)
                { }
            }

            return url;
        }

        private static void DisplayRareOfTheDay(Object o)
        {
            if (updateRotDTimer != null)
            {
                updateRotDTimer = null;
            }

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

                string layout = todisplay.layout;
                if(string.Equals(layout, "normal", StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }
                
                codUsedCards.Add(todisplay.name.ToUpper());

                var cardData = "MTG Bot - Card of the day<br/>" + GenerateCardData(todisplay.name, set.name);
                Program.Messenger.SendMessage(cardData, RoomColors.Yellow);
                cardOfTheDayFound = true;
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
            if (updateCotDTimer != null)
            {
                updateCotDTimer = null;
            }

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

                CotD = new CotD();
                CotD.display = "MTG Bot - Card of the day<br/><img src=" + prepareCardImage(set, todisplay, true) + " />";
                CotD.card = todisplay;

                codUsedCards.Add(todisplay.name.ToUpper());

                Program.Messenger.SendMessage(CotD.display, RoomColors.Green);
                cardOfTheDayFound = true;
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
            targetTime = targetTime.AddMinutes(30);
            targetTime = targetTime.AddSeconds(59 - targetTime.Second);
            targetTime = targetTime.AddMilliseconds(1000 - targetTime.Millisecond);

            var timeDiff = targetTime - DateTime.Now;

            updateCotDTimer = new Timer(DisplayCardOfTheDay, null, (int)timeDiff.TotalMilliseconds, System.Threading.Timeout.Infinite);
        }


        private static string GenerateCardData(string cardData, string setData, bool showRulings=false)
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

            var latestCardSet = cardJson.Values.LastOrDefault(q => q.cards.Any(p => p.name.ToUpper() == cardName.ToUpper()));
            if (setData != "")
            {
                var altLatestCardSet = cardJson.Values.LastOrDefault(q => q.cards.Any(p => p.name.ToUpper() == cardName.ToUpper()) && setData == q.name);
                if (altLatestCardSet != null)
                {
                    latestCardSet = altLatestCardSet;
                }
            }

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
                    cards = FuzzyMatch.FuzzyMatch2(cardJson, cardName, numResults);
                card = cards[0].card;
                html += displayCard(cardJson.Values.Where(p=>p.cards.Contains(card)).First(), card, 311, 223);
                longForm = true;
            }

            if (longForm)
            {
                if (cards == null)
                    cards = FuzzyMatch.FuzzyMatch2(cardJson, cardName, numResults);
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
            
            return "Card Not Recognized. Did you mean?..." + FuzzyMatch.BestMatch2(cardJson, cardName);
        }

        private static string getHtmlText(Card card)
        {
            if (card.text == null)
            {
                return "";
            }

            string widthAlignedText = widthAlign(card.text);

            List<string> ignoreList = new List<string>();

            foreach (Match match in Regex.Matches(widthAlignedText, regexPatternManaOrTapSymbol))
            {
                string value = match.Value;

                if (ignoreList.Contains(value))
                {
                    continue;
                }
                ignoreList.Add(value);

                string switchSymbol = match.Value;
                symbolReplacement.TryGetValue(value, out switchSymbol);
                widthAlignedText = widthAlignedText.Replace(match.Value, switchSymbol);
            }

            return String.Format("<td>{0}<br/><br/>{1}<br/><br/>{2}<br/></td>", card.type, card.rarity, widthAlignedText);
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

        public static string SetData
        { private get; set; }

        public static string setSetToUse(string setName, string requestingUser)
        {
            setName = setName.Replace("((", "");
            setName = setName.Replace("))", "");
            SetData = setName;
            return "<b>Cards from " + SetData + ":</b>";
        }

        private static string getCard(string cardName, string requestingUser)
        {
            cardName = cardName.Replace("{{", "");
            cardName = cardName.Replace("}}", "");
            return GenerateCardData(cardName, SetData);
        }

        private static string doCardOfTheDay(string cardName, string requestingUser)
        {
            if (cardName.ToLower().Equals("score"))
            {
                List<Player> players = new List<Player>();
                Program.AzureStorage.Populate<Player>(out players, TableName, Program.Messenger.Room);

                string ret = "Current Player Scores are:<br><table>";

                foreach(var player in players)
                {
                    ret += "<tr><td>" + player.RowKey + "</td><td></td><td>" + player.CotDScore.ToString() + "</td></tr>";
                }

                ret += "</table>";

                return ret;
            }

            if (CotD == null)
                return requestingUser + " was Too Late!";

            if(cardName.ToLower().Equals("show"))
            {
                return CotD.display;
            }

            if (cardName.ToLower().Equals( CotD.card.name.ToLower()))
            {
                string ret = requestingUser + " Success<br>" + displayCard(cardJson.Values.Where(p => p.cards.Contains(CotD.card)).First(), CotD.card, 311, 223);

                List<Player> players = new List<Player>();
                Program.AzureStorage.Populate<Player>(out players, TableName, Program.Messenger.Room);
                Player player = players.Where(p => p.RowKey.Equals(requestingUser)).FirstOrDefault();

                if(player == null)
                {
                    player = new Player();
                    player.PartitionKey = Program.Messenger.Room;
                    player.RowKey = requestingUser;
                    player.CotDScore = 0;
                }

                player.CotDScore += 1;

                Program.AzureStorage.UploadTableData(player, TableName);

                CotD = null;
                return ret;
            }

            return requestingUser + " " + cardName + " (Incorrect)";
        }

        private static bool doMatch(Card card, Dictionary<string, string> search)
        {
            foreach(KeyValuePair<string, string> pair in search)
            {
                if (pair.Key == "name")
                {
                    if(!card.name.ToLower().Contains(pair.Value))
                    {
                        return false;
                    }
                }
                else if (pair.Key == "manacost")
                {
                    if(!card.manaCost.ToLower().Contains(pair.Value))
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
                else if (pair.Key == "type")
                {
                    if (!card.type.ToLower().Contains(pair.Value))
                    {
                        return false;
                    }
                }
                else if (pair.Key == "printings")
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
                else if (pair.Key == "subtype")
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
                    string[] types = pair.Value.Split(',');
                    foreach (string type in types)
                    {
                        if (card.types == null || !card.types.Contains(type, StringComparer.CurrentCultureIgnoreCase))
                        {
                            return false;
                        }
                    }
                }
                else if (pair.Key == "colouridentity")
                {
                    string[] types = pair.Value.Split(',');
                    foreach (string type in types)
                    {
                        if (card.colorIdentity == null || !card.colorIdentity.Contains(type, StringComparer.CurrentCultureIgnoreCase))
                        {
                            return false;
                        }
                    }
                }
                else if (pair.Key == "text")
                {
                    if (card.text==null || !card.text.ToLower().Contains(pair.Value))
                    {
                        return false;
                    }
                }
                else if (pair.Key == "colour")
                {
                    if (card.colors == null && pair.Value != "none")
                    {
                        return false;
                    }
                    else if (card.colors!=null && !card.colors.Contains(pair.Value, StringComparer.CurrentCultureIgnoreCase))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static string doSearch(Dictionary<string,string> search, string requestingUser)
        {
            Dictionary<string, Card> cardsFound = new Dictionary<string, Card>();
            foreach(SetData set in cardJson.Values)
            {
                foreach(Card card in set.cards)
                {
                    if(doMatch(card, search))
                    {
                        if (!cardsFound.ContainsKey(card.name))
                        {
                            cardsFound.Add(card.name, card);
                        }
                        else
                        {
                            if(cardsFound[card.name].multiverseid < card.multiverseid)
                            {
                                cardsFound[card.name] = card;
                            }
                        }
                    }
                }
            }
            string returnVal = "<table>";
            int count = 0;
            foreach (Card card in cardsFound.Values.OrderByDescending(p=>p.multiverseid))
            {
                if(count%3 == 0)
                {
                    returnVal += "<tr>";
                }
                returnVal += "<td>";
                returnVal += displayCard(cardJson.Values.Where(p => p.cards.Contains(card)).First(), card, 210, 150);
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
