using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Organiser")]
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
    }
}