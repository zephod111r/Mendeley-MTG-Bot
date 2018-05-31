using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.WindowsAzure.Storage.Table;

namespace MTGWebJob
{
    class UsedCotD : TableEntry
    {
        private const string tableName = "cotdcards";
        internal override string TableName { get { return tableName; } }
        public DateTime DateShown { get; set; }
        public string GuessingPlayer { get; set; }
        public DateTime DateGuessed { get; set; }
        public string Name {  get { return RowKey;  }  set { RowKey = value; } }
        static internal bool AlreadyUsed(Card todisplay)
        {
            UsedCotD nameAlreadyUsed = Get(todisplay);
            return nameAlreadyUsed != null;
        }
        static internal UsedCotD Get(Card card)
        {
            string query2 = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Program.Messenger.Room);
            query2 = TableQuery.CombineFilters(query2, TableOperators.And, TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, card.name));
            UsedCotD output = Program.AzureStorage.Populate<UsedCotD>(tableName, query2);
            return output;
        }
    }
}
