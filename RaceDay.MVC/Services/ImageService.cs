using System.Net.Http.Headers;
using System.Text.Json;

namespace RaceDay.MVC.Services
{
    public class ImageService
    {
        private readonly ApiService _apiService;

        public ImageService(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<string?> UploadImageAsync(IFormFile image)
        {
            var client = _apiService.GetAuthenticatedClient();

            using var formData = new MultipartFormDataContent();

            await using var stream = image.OpenReadStream();

            using var fileContent = new StreamContent(stream);

            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue(image.ContentType);

            formData.Add(fileContent, "image", image.FileName);

            var response = await client.PostAsync(
                "api/Images/upload", formData);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Image upload failed: {error}");
            }

            var responseContent =
                await response.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(responseContent);

            return document.RootElement
                .GetProperty("imageUrl")
                .GetString();
        }
        public async Task<(byte[] Content, string ContentType)?> DownloadImageAsync(
    string fileName)
        {
            var client = _apiService.GetAuthenticatedClient();

            var response = await client.GetAsync(
                $"api/Images/download/{Uri.EscapeDataString(fileName)}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsByteArrayAsync();

            var contentType =
                response.Content.Headers.ContentType?.MediaType
                ?? "application/octet-stream";

            return (content, contentType);
        }
    }
}