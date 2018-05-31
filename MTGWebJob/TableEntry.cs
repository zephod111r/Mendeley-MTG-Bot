using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace MTGWebJob
{
    class TableEntry : TableEntity
    {
        public TableEntry() : base(Program.Messenger.Room, null) { }
        public TableEntry(String row) : base(Program.Messenger.Room, row) { }
        virtual internal string TableName { get; }
        public int Version { get; set; }

        virtual internal void Save()
        {
            Program.AzureStorage.UploadTableData(this, TableName);
        }

        internal virtual void UpdateVersion() { }
    }
}
