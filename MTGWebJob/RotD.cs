using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace MTGWebJob
{
    class RotD : TableEntry
    {
        private static Random localRandom = new Random();
        private static Timer updateRotDTimer = null;
        private const String tableName = "rotd";


        public RotD() : base() { }
        public RotD(string name) : base(Program.Messenger.Room, name) { }
        internal override string TableName {  get { return tableName;  } }
        public DateTime DateShown { get; set; }
        public string CardName { get { return this.RowKey; } set { this.RowKey = value; } }
        static internal bool AlreadyUsed(Card todisplay)
        {
            string query = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Program.Messenger.Room);
            query = TableQuery.CombineFilters(query, TableOperators.And, TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, todisplay.name));
            RotD nameAlreadyUsed = Program.AzureStorage.Populate<RotD>(tableName, query);
            return nameAlreadyUsed != null;
        }
        
        internal static void DisplayRareOfTheDay(Object o)
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


                    if (RotD.AlreadyUsed(todisplay))
                    {
                        continue;
                    }

                    codUsedCards.Add(todisplay.name.ToUpper());

                    RotD card = new RotD();
                    card.CardName = todisplay.name;
                    card.DateShown = DateTime.Now.ToLocalTime();
                    card.Save();

                    List<SetData> sets = new List<SetData>();
                    sets.Add(set);
                    var cardData = "MTG Bot - Card of the day<br/>" + GenerateCardData(todisplay.name, sets);
                    Program.Messenger.SendMessage(cardData, MessageClient.MessageColour.Yellow);
                    cardOfTheDayFound = true;
                }
            }

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
            targetTime = targetTime.AddSeconds(59 - targetTime.Second);
            targetTime = targetTime.AddMilliseconds(1000 - targetTime.Millisecond);

            var timeDiff = targetTime - DateTime.Now.ToLocalTime();
            updateRotDTimer = new Timer(DisplayRareOfTheDay, null, (int)timeDiff.TotalMilliseconds, System.Threading.Timeout.Infinite);
        }
    }
}
