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
            // Check that an image was provided.
            if (image == null || image.Length == 0)
            {
                throw new ArgumentException("Please select an image.");
            }

            // Limit image size to 5 MB.
            if (image.Length > 5 * 1024 * 1024)
            {
                throw new ArgumentException(
                    "Image size cannot exceed 5 MB.");
            }

            // Only allow JPEG and PNG images.
            var allowedTypes = new[]
            {
                "image/jpeg",
                "image/png"
            };

            if (!allowedTypes.Contains(image.ContentType.ToLowerInvariant()))
            {
                throw new ArgumentException(
                    "Only JPG, JPEG and PNG images are allowed.");
            }

            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

            if (extension != ".jpg" &&
                extension != ".jpeg" &&
                extension != ".png")
            {
                throw new ArgumentException(
                    "Invalid image file extension.");
            }

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