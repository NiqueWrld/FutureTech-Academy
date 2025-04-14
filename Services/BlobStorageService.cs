using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace FutureTech_Academy.Services
{
    public class BlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _connectionString;
        private readonly string _containerName;

        public BlobStorageService(IConfiguration configuration)
        {
            _connectionString = configuration["AzureBlobStorage:ConnectionString"];
            _containerName = configuration["AzureBlobStorage:ContainerName"];
            _blobServiceClient = new BlobServiceClient(_connectionString);
            
            CreateContainerIfNotExistsAsync().GetAwaiter().GetResult();
        }

        private async Task CreateContainerIfNotExistsAsync()
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
                Console.WriteLine($"Container '{_containerName}' created or already exists with public access");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating container: {ex.Message}");
                throw;
            }
        }

        private BlobContainerClient GetBlobContainer(string containerName)
        {

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException("StorageConnectionString is empty or missing in appsettings.json.");
            }


            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            // Ensure the container exists
            containerClient.CreateIfNotExists();

            return containerClient;
        }

        public async Task<string> UploadPhotoAsync(Stream fileStream, string fileName)
        {
            var containerClient = GetBlobContainer(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            await blobClient.UploadAsync(fileStream, overwrite: true);

            var expiryTime = DateTimeOffset.UtcNow.AddHours(1);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = fileName,
                ExpiresOn = expiryTime
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read | BlobSasPermissions.Write); 

            var storageAccount = GetStorageAccount(); 
            var sasToken = blobClient.GenerateSasUri(sasBuilder).Query;

            var sasUrl = blobClient.Uri + sasToken;

            return sasUrl;
        }

        public BlobServiceClient GetStorageAccount()
        {
 
            var connectionString = "DefaultEndpointsProtocol=https;AccountName=deletethisoneboy;AccountKey=q36llPtsRLtGNmfZH9KBns/8E/F92qYlKfAE3Zi427Xoy4ogy4oTlO3DpKriwPDK4EeBC4gkEvSn+AStOAhXvw==;EndpointSuffix=core.windows.net"; 

            var blobServiceClient = new BlobServiceClient(connectionString);

            return blobServiceClient;
        }


        public string GenerateBlobSasToken(string containerName, string fileName)
        {
            var containerClient = GetBlobContainer(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = fileName,
                Resource = "b"
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);
            sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddHours(1);

            var sasToken = blobClient.GenerateSasUri(sasBuilder).Query;

            return sasToken;
        }

        public async Task DeleteBlobAsync(string containerName, string fileName)
        {
            var containerClient = GetBlobContainer(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.DeleteIfExistsAsync();
        }


    }
} 