using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ImagesController : ControllerBase
    {
        private readonly AzureBlobStorageService _blobStorageService;

        public ImagesController(
            AzureBlobStorageService blobStorageService)
        {
            _blobStorageService = blobStorageService;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UploadImage(
    IFormFile image)
        {
            try
            {
                var imageUrl =
                    await _blobStorageService.UploadImageAsync(
                        image,
                        "event-images");

                return Ok(new
                {
                    message = "Image uploaded successfully.",
                    imageUrl = imageUrl
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }
        [HttpGet("download/{fileName}")]
        [Authorize]
        public async Task<IActionResult> DownloadImage(string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName) ||
                    fileName != Path.GetFileName(fileName))
                {
                    return BadRequest("Invalid image filename.");
                }

                var image = await _blobStorageService.DownloadImageAsync(
                    fileName,
                    "event-images");

                return File(image.Content, image.ContentType);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                return NotFound("Image not found.");
            }
        }
    }
}