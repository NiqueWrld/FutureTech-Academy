using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;

namespace FutureTech_Academy.Services
{
    public class BlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;

        public BlobStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureBlobStorage:ConnectionString"];
            _containerName = configuration["AzureBlobStorage:ContainerName"];
            _blobServiceClient = new BlobServiceClient(connectionString);
            
            // Ensure container exists with public access
            CreateContainerIfNotExistsAsync().GetAwaiter().GetResult();
        }

        private async Task CreateContainerIfNotExistsAsync()
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                
                // Create container with public access
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
                
                // Set CORS rules
                var properties = await _blobServiceClient.GetPropertiesAsync();
                var corsRules = new List<BlobCorsRule>
                {
                    new BlobCorsRule
                    {
                        AllowedHeaders = "*",
                        AllowedMethods = "GET,PUT,POST,DELETE,HEAD,OPTIONS",
                        AllowedOrigins = "*",
                        ExposedHeaders = "*",
                        MaxAgeInSeconds = 3600
                    }
                };
                
                properties.Value.Cors = corsRules;
                await _blobServiceClient.SetPropertiesAsync(properties);
                
                Console.WriteLine($"Container '{_containerName}' created or already exists with public access and CORS configured");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating container: {ex.Message}");
                throw;
            }
        }

        public async Task<string> UploadFileAsync(IFormFile file, string fileName)
        {
            try
            {
                Console.WriteLine($"Starting file upload: {fileName}");
                
                // Get a reference to the container
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                
                // Generate a unique blob name
                var blobName = $"{Guid.NewGuid()}-{fileName}";
                var blobClient = containerClient.GetBlobClient(blobName);
                
                Console.WriteLine($"Uploading to blob: {blobName}");

                // Upload the file
                using (var stream = file.OpenReadStream())
                {
                    var options = new BlobUploadOptions
                    {
                        HttpHeaders = new BlobHttpHeaders 
                        { 
                            ContentType = file.ContentType,
                            CacheControl = "public, max-age=31536000"
                        },
                        AccessTier = AccessTier.Hot
                    };
                    
                    await blobClient.UploadAsync(stream, options);
                }

                // Get the URL of the uploaded file
                var url = blobClient.Uri.ToString();
                Console.WriteLine($"File uploaded successfully. URL: {url}");
                
                return url;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading file to Blob Storage: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(fileUrl))
                    return;

                Console.WriteLine($"Attempting to delete file: {fileUrl}");

                // Get the blob name from the URL
                var uri = new Uri(fileUrl);
                var blobName = uri.Segments.Last();

                // Get a reference to the container and blob
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                // Delete the blob if it exists
                var response = await blobClient.DeleteIfExistsAsync();
                Console.WriteLine($"File deletion {(response.Value ? "successful" : "failed - file not found")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file from Blob Storage: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }
    }
} 