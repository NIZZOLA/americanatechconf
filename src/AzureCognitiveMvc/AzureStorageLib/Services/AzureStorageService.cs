using AzureStorageLib.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageLib.Services
{
    public class AzureStorageService
    {
        public static async Task<bool> UploadFileToStorage(AzureStorageConfig _storageConfig, Stream fileStream, string fileName)
        {
            try
            {

                // Create storagecredentials object by reading the values from the configuration (appsettings.json)
                StorageCredentials storageCredentials = new StorageCredentials(_storageConfig.AccountName, _storageConfig.AccountKey);

                // Create cloudstorage account by passing the storagecredentials
                CloudStorageAccount storageAccount = new CloudStorageAccount(storageCredentials, true);

                // Create the blob client.
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                // Get reference to the blob container by passing the name by reading the value from the configuration (appsettings.json)
                //CloudBlobContainer container = blobClient.GetContainerReference(_storageConfig.ImageContainer);
                var container = blobClient.GetContainerReference(_storageConfig.ImageContainer);
                container.CreateIfNotExistsAsync().Wait();

                // Get the reference to the block blob from the container
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);

                // Upload the file
                await blockBlob.UploadFromStreamAsync(fileStream);

            }
            catch (Exception error)
            {

                throw;
            }
            return await Task.FromResult(true);
        }

        public static string BlobUrl(AzureStorageConfig _storageConfig, string imageFileName)
        {
            var account = new CloudStorageAccount(new StorageCredentials(_storageConfig.AccountName, _storageConfig.AccountKey), true);
            var cloudBlobClient = account.CreateCloudBlobClient();
            var container = cloudBlobClient.GetContainerReference(_storageConfig.ImageContainer);
            var blob = container.GetBlockBlobReference(imageFileName);
            //blob.UploadFromFile("File Path ....");//Upload file....

            return blob.Uri.AbsoluteUri;
        }

        public static async Task<bool> DeleteSpecificBlob(AzureStorageConfig _storageConfig, string blobName)
        {
            StorageCredentials storageCredentials = new StorageCredentials(_storageConfig.AccountName, _storageConfig.AccountKey);

            // Create cloudstorage account by passing the storagecredentials
            CloudStorageAccount storageAccount = new CloudStorageAccount(storageCredentials, true);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer container = blobClient.GetContainerReference(_storageConfig.ImageContainer);

            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

            if (await blockBlob.DeleteIfExistsAsync())
            {
                return true;
            }
            return false;
        }

        public static async Task<List<string>> GetThumbNailUrls(AzureStorageConfig _storageConfig)
        {
            List<string> thumbnailUrls = new List<string>();

            // Create storagecredentials object by reading the values from the configuration (appsettings.json)
            StorageCredentials storageCredentials = new StorageCredentials(_storageConfig.AccountName, _storageConfig.AccountKey);

            // Create cloudstorage account by passing the storagecredentials
            CloudStorageAccount storageAccount = new CloudStorageAccount(storageCredentials, true);

            // Create blob client
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Get reference to the container
            CloudBlobContainer container = blobClient.GetContainerReference(_storageConfig.ThumbnailContainer);

            BlobContinuationToken continuationToken = null;

            BlobResultSegment resultSegment = null;

            //Call ListBlobsSegmentedAsync and enumerate the result segment returned, while the continuation token is non-null.
            //When the continuation token is null, the last page has been returned and execution can exit the loop.
            do
            {
                //This overload allows control of the page size. You can return all remaining results by passing null for the maxResults parameter,
                //or by calling a different overload.
                resultSegment = await container.ListBlobsSegmentedAsync("", true, BlobListingDetails.All, 10, continuationToken, null, null);

                foreach (var blobItem in resultSegment.Results)
                {
                    thumbnailUrls.Add(blobItem.StorageUri.PrimaryUri.ToString());
                }

                //Get the continuation token.
                continuationToken = resultSegment.ContinuationToken;
            }

            while (continuationToken != null);

            return await Task.FromResult(thumbnailUrls);
        }

    }
}
