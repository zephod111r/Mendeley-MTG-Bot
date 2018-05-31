using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Queue;

namespace MTGWebJob
{
    class Azure
    {
        private Dictionary<string, CloudBlobContainer> Blobs = new Dictionary<string, CloudBlobContainer>();
        private Dictionary<string, CloudTable> Tables = new Dictionary<string, CloudTable>();

        CloudBlobClient BlobClient = null;
        CloudTableClient TableClient = null;
        CloudQueueClient QueueClient = null;

        private const string DateFormat = "yyyyMMdd ; HH:mm:ss:fffffff";
        private const string RowKeyFormat = "{0} - {1}";

        public string StorageKey
        {
            set
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(value);
                BlobClient = storageAccount.CreateCloudBlobClient();
                TableClient = storageAccount.CreateCloudTableClient();
                QueueClient = storageAccount.CreateCloudQueueClient();

                RefreshTables();
            }
        }

        CloudBlobContainer EnsureBlobPresence(string blobName)
        {
            try
            {
                string fixedBlobName = blobName.ToLower();
                fixedBlobName = fixedBlobName.Replace("+", "-");
                fixedBlobName = fixedBlobName.Replace(".", "");
                fixedBlobName = fixedBlobName.Replace("(", "");
                fixedBlobName = fixedBlobName.Replace(")", "");
                Blobs[blobName] = BlobClient.GetContainerReference(fixedBlobName);
                Blobs[blobName].CreateIfNotExists();
                var blobPerm = new BlobContainerPermissions();
                blobPerm.PublicAccess = BlobContainerPublicAccessType.Container;
                Blobs[blobName].SetPermissions(blobPerm);
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

        public string Upload(System.IO.Stream output, string name, string table)
        {
            CloudBlockBlob blockBlob = EnsureBlobPresence(table).GetBlockBlobReference(name.ToLower());
            blockBlob.UploadFromStream(output, output.Length);
            return blockBlob.Uri.ToString();
        }

        public string IsBlobPresent(string name, string table)
        {
            CloudBlockBlob blockBlob = EnsureBlobPresence(table).GetBlockBlobReference(name.ToLower());
            if (blockBlob.Exists())
            {
                return blockBlob.Uri.ToString();
            }
            return "";
        }

        public System.IO.Stream Download(string set, string name, string rootName)
        {
            CloudBlockBlob blockBlob = EnsureBlobPresence(set).GetBlockBlobReference(name.ToLower());
            System.IO.Stream stream = new System.IO.MemoryStream();
            blockBlob.DownloadToStream(stream);
            return stream;
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

        public void RemoveTableData(ITableEntity element, string tableName)
        {
            CloudTable table = EnsureTablePresence(tableName);

            try
            {
                var insertOperation = TableOperation.Delete(element);

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

        public typeOfEntity Populate<typeOfEntity>(string tableName, string queryString)
            where typeOfEntity : TableEntity, new()
        {
            try
            {
                CloudTable table = EnsureTablePresence(tableName);
                TableQuery<typeOfEntity> query = new TableQuery<typeOfEntity>().Where(queryString);
                return table.ExecuteQuery<typeOfEntity>(query).FirstOrDefault();
            }
            catch (StorageException err)
            {
                Console.Out.WriteLineAsync(err.Message);
                return null;
            }
        }

        public void AddQueueItem(string nameOfQueue, string Json)
        {
            try
            {
                byte[] forBase64 = Encoding.UTF8.GetBytes(Json);
                Console.WriteLine("Writing to " + nameOfQueue + " : " + Json);
                CloudQueue cloudQueue = QueueClient.GetQueueReference(nameOfQueue);
                cloudQueue.CreateIfNotExists();
                CloudQueueMessage message = new CloudQueueMessage(forBase64);
                cloudQueue.AddMessage(message);
                Console.WriteLine(" ... Success!");
            }
            catch (Exception)
            {
                Console.Error.WriteLine(" ... Failed!");
            }
        }
    }
}
