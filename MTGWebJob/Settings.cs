using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;

namespace MTGWebJob
{
    class Setting : TableEntry
    {
        private const String tableName = "setting";
        internal override String TableName { get { return tableName; } }

        public Setting() : base(Program.Messenger.Room, "") { }
        public Setting(string name) : base(Program.Messenger.Room, name) { }
        public string Name { get { return RowKey; } set { RowKey = value; } }

        static internal typeOfEntity Get<typeOfEntity>(String name)
            where typeOfEntity : TableEntity, new()
        {
            string query2 = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Program.Messenger.Room);
            query2 = TableQuery.CombineFilters(query2, TableOperators.And, TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, name));
            typeOfEntity output = Program.AzureStorage.Populate<typeOfEntity>(tableName, query2);
            return output;
        }
    }

    class SettingBool : Setting
    {
        public SettingBool(String name) : base(name) { }
        public SettingBool() : base() { }

        public bool Value { get; set; }

    }

    class SettingInt : Setting
    {
        public SettingInt(String name) : base(name) { }
        public SettingInt() : base() { }

        public int Value { get; set; }

    }

    class SettingString : Setting
    {
        public SettingString(String name) : base(name) { }
        public SettingString() : base() { }

        public string Value { get; set; }

    }

    class SettingGuid : Setting
    {
        public SettingGuid(String name) : base(name) { }
        public SettingGuid() : base() { }

        public Guid Value { get; set; }

    }
}
