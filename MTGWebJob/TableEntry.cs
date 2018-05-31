using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace MTGWebJob
{
    class TableEntry : TableEntity
    {
        public TableEntry() : base() { }
        public TableEntry(String partition, String row) : base(partition, row) { }
        virtual internal string TableName { get; }
        public int Version { get; set; }

        virtual internal void Save()
        {
            Program.AzureStorage.UploadTableData(this, TableName);
        }

        internal virtual void UpdateVersion() { }
    }
}
