using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers
{
    public class ParticipantController : Controller
    {
        private readonly EventService _eventService;
        private readonly CategoryService _categoryService;

        public ParticipantController(
            EventService eventService,
            CategoryService categoryService)
        {
            _eventService = eventService;
            _categoryService = categoryService;
        }

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.FullName = HttpContext.Session.GetString("FullName");

            try
            {
                var events = await _eventService.GetEventsAsync();
                return View(events);
            }
            catch
            {
                ViewBag.ErrorMessage =
                    "Unable to load events. Please try again later.";

                return View(new List<RaceDay.MVC.ViewModels.EventViewModel>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var eventDetails = await _eventService.GetEventByIdAsync(id);

                if (eventDetails == null)
                {
                    return NotFound();
                }

                return View(eventDetails);
            }
            catch
            {
                return NotFound();
            }
        }

        public async Task<IActionResult> Categories(int eventId)
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var categories =
                    await _categoryService.GetCategoriesByEventAsync(eventId);

                ViewBag.EventID = eventId;

                return View(categories);
            }
            catch
            {
                ViewBag.ErrorMessage =
                    "Unable to load categories. Please try again later.";

                return View(
                    new List<RaceDay.MVC.ViewModels.CategoryViewModel>());
            }
        }
    }
}