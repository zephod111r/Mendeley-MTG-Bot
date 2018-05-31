using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace MTGWebJob
{
    class CotD : TableEntry
    {
        private static Timer updateCotDTimer = null;
        private const string tableName = "CotD";
        internal override string TableName { get { return tableName; } }


        public SetData set { get; set; }
        public Card card { get; set; }
        public string display { get; set; }

        internal static void DisplayCardOfTheDay(Object o)
        {
            SettingString cotdId = Setting.Get<SettingString>("CotD");
            CotD cotd = Get(cotdId.Value);
            try
            {
                if (cotd != null)
                {
                    string message = "CotD: No one guessed correctly!<br>It was:<br>" + displayCard(MagicTheGathering.cardJson.Values.Where(p => p.cards.Contains(cotd.card)).First(), cotd.card, 311, 223);
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

                    List<SetData> setsToLookin = MagicTheGathering.cardJson.Values.Where(p => !setImageFilter.Contains(p.name, StringComparer.InvariantCultureIgnoreCase)).ToList();

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

                        if (UsedCotD.AlreadyUsed(todisplay))
                        {
                            continue;
                        }

                        UsedCotD card = new UsedCotD();
                        card.PartitionKey = Program.Messenger.Room;
                        card.RowKey = todisplay.name;
                        card.DateShown = DateTime.Now.ToLocalTime();
                        card.GuessingPlayer = null;
                        card.DateGuessed = DateTime.Now.ToLocalTime();
                        card.Save();

                        cardOfTheDayFound = true;
                        cotd = new CotD();
                        cotd.display = "MTG Bot - Card of the day<br/><img src=" + ImageUtility.prepareCardImage(set, todisplay, true) + " />";
                        cotd.card = todisplay;
                        cotd.set = set;

                        codUsedCards.Add(todisplay.name.ToUpper());

                        Program.Messenger.SendMessage(cotd.display, MessageClient.MessageColour.Green);
                    }
                }
            }
            catch (Exception) { }
            finally
            {
                var targetTime = DateTime.Now.ToLocalTime();
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

                var timeDiff = targetTime - DateTime.Now.ToLocalTime();

                updateCotDTimer = new Timer(DisplayCardOfTheDay, null, (int)timeDiff.TotalMilliseconds, System.Threading.Timeout.Infinite);
            }
        }

        private static CotD Get(string name)
        {
            string query = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Program.Messenger.Room);
            query = TableQuery.CombineFilters(query, TableOperators.And, TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, name));
            return Program.AzureStorage.Populate<CotD>(tableName, query);
        }
    }
}
