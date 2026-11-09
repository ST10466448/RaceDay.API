using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.API.Services
{
    public class AzureBlobStorageService
    {
        private readonly IConfiguration _configuration;

        public AzureBlobStorageService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> UploadImageAsync(
            IFormFile image,
            string containerName)
        {
            var connectionString =
                _configuration.GetConnectionString("AzureBlobStorage");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Azure Blob Storage connection string is missing.");
            }

            var blobServiceClient =
                new BlobServiceClient(connectionString);

            var containerClient =
                blobServiceClient.GetBlobContainerClient(containerName);

            await containerClient.CreateIfNotExistsAsync();

            var extension = Path.GetExtension(image.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";

            var blobClient =
                containerClient.GetBlobClient(fileName);

            await using var stream = image.OpenReadStream();

            await blobClient.UploadAsync(
                stream,
                new BlobHttpHeaders
                {
                    ContentType = image.ContentType
                });

            return blobClient.Uri.ToString();
        }
    }
}