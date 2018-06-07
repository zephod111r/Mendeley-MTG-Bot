using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;

namespace MTGWebJob
{
    class Season : TableEntry
    {
        private const string tableName = "season";
        internal override string TableName { get { return tableName; } }

        public Season() : base(Guid.NewGuid().ToString()) { WinningCount = 50; }
        public int WinningCount { get; set; }
        public string Winner { get; set; }
        public bool InProgress { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string Id { get { return RowKey; } }

        static internal Season Get()
        {
            SettingString seasonId = Setting.Get<SettingString>(tableName);
            Season currentSeason = Get(seasonId.Value);

            if (currentSeason == null)
            {
                currentSeason = new Season();
                currentSeason.StartDate = DateTime.Now;
                currentSeason.EndDate = DateTime.Now.AddMonths(12);
                seasonId.Value = currentSeason.Id;
                currentSeason.Save();
                seasonId.Save();
            }

            return currentSeason;
        }

        static internal Season Get(String seasonId)
        {
            string query2 = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Program.Messenger.Room);
            query2 = TableQuery.CombineFilters(query2, TableOperators.And, TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, seasonId));
            Season output = Program.AzureStorage.Populate<Season>(tableName, query2);
            return output;
        }

        static internal void Get(out List<Season> seasons)
        {
            Program.AzureStorage.Populate<Season>(out seasons, tableName, Program.Messenger.Room);
        }

        internal static string EndSeason()
        {
            SettingString seasonId = Setting.Get<SettingString>(tableName);
            Season oldSeason = Get();
            if (oldSeason != null)
            {
                oldSeason.EndDate = DateTime.Now;
                oldSeason.Save();
            }
            Season newSeason = new Season();
            seasonId.Value = newSeason.Id;
            newSeason.StartDate = DateTime.Now;
            newSeason.Save();
            seasonId.Save();
            return "<b>New Season Started!</b>";
        }
    }
}
