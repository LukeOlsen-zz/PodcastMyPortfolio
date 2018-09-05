using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ClientManagement.Data
{
    public static class DataContextExtensions
    {
        public static async Task<string> SaveAudioFileFromForm(string conntection, string containerName, string fileId, MemoryStream ms)
        {
            return await SaveFileFromForm(conntection, containerName, "audio/mp3", fileId, ms);
        }

        public static async Task<string> SaveFileFromForm(string conntection, string containerName, string contentType, string fileId, MemoryStream ms)
        {
            if (ms != null)
            {
                // Copy binary to Azure and place reference into database
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(conntection);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                CloudBlobContainer container = blobClient.GetContainerReference(containerName);
                await container.CreateIfNotExistsAsync();
                await container.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

                if (String.IsNullOrWhiteSpace(fileId))
                    fileId = Guid.NewGuid().ToString();

                CloudBlockBlob blob = container.GetBlockBlobReference(fileId);
                blob.Properties.ContentType = contentType;

                ms.Position = 0;
                await blob.UploadFromStreamAsync(ms);

                return fileId;
            }
            else
                return null;
        }

        public static async Task DeleteFile(string connection, string containerName, string fileId)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connection);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            CloudBlockBlob existingBlob = container.GetBlockBlobReference(fileId);
            await existingBlob.DeleteIfExistsAsync();
        }
    }
}
