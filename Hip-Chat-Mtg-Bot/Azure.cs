using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace HipchatMTGBot
{
    class Azure
    {
        private CloudBlobContainer Container
        {
            get; set;
        }

        public string StorageKey
        {
            set
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(value);
                CloudBlobClient fileClient = storageAccount.CreateCloudBlobClient();
                Container = fileClient.GetContainerReference("cotd");
                Container.CreateIfNotExists();
                var perm = new BlobContainerPermissions();
                perm.PublicAccess = BlobContainerPublicAccessType.Container;
                Container.SetPermissions(perm);
            }
        }

        public string Upload(string file)
        {
            CloudBlockBlob blockBlob = Container.GetBlockBlobReference(file);
            using (var fileStream = System.IO.File.OpenRead(file))
            {
                blockBlob.UploadFromStream(fileStream);
            }
            return blockBlob.Uri.ToString();
        }
    }
}
