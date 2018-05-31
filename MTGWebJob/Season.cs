using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;

namespace MTGWebJob
{
    class Season : TableEntry
    {
        public Season() : base() { WinningCount = 50; }
        static internal string SettingName { get { return tableName; } }
        private const string tableName = "season";
        internal override string TableName { get { return tableName; } }
        public int WinningCount { get; set; }
        public string Winner { get; set; }
        public bool InProgress { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        static internal Season Get(String seasonId)
        {
            string query2 = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Program.Messenger.Room);
            query2 = TableQuery.CombineFilters(query2, TableOperators.And, TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, seasonId));
            Season output = Program.AzureStorage.Populate<Season>(tableName, query2);
            return output;
        }
        internal static string EndSeason()
        {
            SettingString SettingString = Setting.Get<SettingString>(Season.SettingName);
            Season newSeason = new Season();
            SettingString.Value = newSeason.RowKey;
            newSeason.Save();
            SettingString.Save();
            return "<h1>New Season Started!</h1>";
        }
    }
}
