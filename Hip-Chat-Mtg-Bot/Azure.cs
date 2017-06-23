using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace HipchatMTGBot
{
    class Azure
    {
        private Dictionary<string, CloudBlobContainer> Blobs = new Dictionary<string, CloudBlobContainer>();
        private Dictionary<string, CloudTable> Tables = new Dictionary<string, CloudTable>();

        CloudBlobClient BlobClient = null;
        CloudTableClient TableClient = null;
        
        private const string DateFormat = "yyyyMMdd ; HH:mm:ss:fffffff";
        private const string RowKeyFormat = "{0} - {1}";

        public string StorageKey
        {
            set
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(value);
                BlobClient = storageAccount.CreateCloudBlobClient();
                TableClient = storageAccount.CreateCloudTableClient();
                RefreshTables();
            }
        }

        CloudBlobContainer EnsureBlobPresence(string blobName)
        {
            try
            {
                string fixedBlobName = blobName.ToLower();
                fixedBlobName = fixedBlobName.Replace("+", "-");
                Blobs[fixedBlobName] = BlobClient.GetContainerReference(fixedBlobName);
                Blobs[fixedBlobName].CreateIfNotExists();
                var blobPerm = new BlobContainerPermissions();
                blobPerm.PublicAccess = BlobContainerPublicAccessType.Container;
                Blobs[fixedBlobName].SetPermissions(blobPerm);
            }
            catch (StorageException err)
            {
                Console.Out.WriteLineAsync(err.Message);
            }

            return Blobs[blobName];
        }

        CloudTable EnsureTablePresence(string tableName)
        {
            try
            {
                Tables[tableName] = TableClient.GetTableReference(tableName);
                Tables[tableName].CreateIfNotExists();
            }
            catch (StorageException err)
            {
                Console.Out.WriteLineAsync(err.Message);
            }
            return Tables[tableName];
        }

        private void RefreshTables()
        {
            foreach (var blobPair in Blobs)
            {
                EnsureBlobPresence(blobPair.Key);
            }
            foreach (var tablePair in Tables)
            {
                EnsureTablePresence(tablePair.Key);
            }
        }

        public string Upload(string name, string table, string fileroot)
        {
            CloudBlockBlob blockBlob = EnsureBlobPresence(table).GetBlockBlobReference(name.ToLower());
            using (var fileStream = System.IO.File.OpenRead(fileroot + "/" + table + "/" + name))
            {
                blockBlob.UploadFromStream(fileStream);
            }
            return blockBlob.Uri.ToString();
        }

        public string IsBlobPresent(string name, string table)
        {
            CloudBlockBlob blockBlob = EnsureBlobPresence(table).GetBlockBlobReference(name.ToLower());
            if (blockBlob.Exists())
                return blockBlob.Uri.ToString();
            return "";
        }

        public bool Download(string set, string name, string table)
        {
            CloudBlockBlob blockBlob = EnsureBlobPresence(table).GetBlockBlobReference(name.ToLower());
            if (blockBlob.Exists())
            {
                blockBlob.DownloadToFile(table + "/" + set + "/" + name, System.IO.FileMode.Create);
                return true;
            }
            return false;
        }

        public void UploadTableData(ITableEntity element, string tableName)
        {
            CloudTable table = EnsureTablePresence(tableName);

            try
            {
                var insertOperation = TableOperation.InsertOrReplace(element);

                // Execute the insert operation.
                TableResult res = table.Execute(insertOperation);

            }
            catch (StorageException err)
            {
                Console.Out.WriteLineAsync(err.Message);
            }
        }

        public void Populate<typeOfEntity>(out List<typeOfEntity> elements, string tableName, string partitionName)
            where typeOfEntity : TableEntity, new()
        {
            try
            {
                CloudTable table = EnsureTablePresence(tableName);
                TableQuery<typeOfEntity> query = new TableQuery<typeOfEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey",
                   QueryComparisons.Equal, partitionName));
                elements = table.ExecuteQuery<typeOfEntity>(query).ToList();
            }
            catch (StorageException err)
            {
                Console.Out.WriteLineAsync(err.Message);
                elements = new List<typeOfEntity>();
            }
        }
    }
}
