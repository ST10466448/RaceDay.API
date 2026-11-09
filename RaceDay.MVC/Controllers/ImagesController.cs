using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers
{
    public class ImagesController : Controller
    {
        private readonly ImageService _imageService;

        public ImagesController(ImageService imageService)
        {
            _imageService = imageService;
        }

        [HttpGet]
        public async Task<IActionResult> Display(string fileName)
        {
            if (string.IsNullOrWhiteSpace(
                HttpContext.Session.GetString("JWToken")))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(fileName) ||
                fileName != Path.GetFileName(fileName))
            {
                return BadRequest();
            }

            try
            {
                var image =
                    await _imageService.DownloadImageAsync(fileName);

                if (image == null)
                {
                    return NotFound();
                }

                return File(image.Value.Content,
                    image.Value.ContentType);
            }
            catch
            {
                return StatusCode(500);
            }
        }
    }
}