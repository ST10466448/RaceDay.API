using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using RaceDay.MVC.ViewModels;
using System.Linq;

namespace RaceDay.MVC.Controllers
{
    public class OrganiserController : Controller
    {
        private readonly EventService _eventService;
        private readonly RouteService _routeService;
        private readonly CategoryService _categoryService;

        public OrganiserController(
            EventService eventService,
            RouteService routeService,
            CategoryService categoryService)
        {
            _eventService = eventService;
            _routeService = routeService;
            _categoryService = categoryService;
        }

        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.FullName = HttpContext.Session.GetString("FullName");

            return View();
        }

        public async Task<IActionResult> ManageEvents()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var events = await _eventService.GetEventsAsync();
                return View(events);
            }
            catch
            {
                ViewBag.ErrorMessage =
                    "Unable to load events. Please try again later.";

                return View(new List<EventViewModel>());
            }
        }

        // Display the Create Event form
        public async Task<IActionResult> CreateEvent()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Routes = await _routeService.GetRoutesAsync();

            return View(new EventViewModel());
        }

        // Submit the Create Event form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(EventViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Routes = await _routeService.GetRoutesAsync();
                return View(model);
            }

            var response = await _eventService.CreateEventAsync(model);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("ManageEvents");
            }

            ModelState.AddModelError(
                "",
                "Unable to create event. Please check your details.");

            ViewBag.Routes = await _routeService.GetRoutesAsync();

            return View(model);
        }

        // Display the Edit Event form
        public async Task<IActionResult> EditEvent(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Account");
            }

            var eventModel = await _eventService.GetEventForEditAsync(id);

            if (eventModel == null)
            {
                return NotFound();
            }

            ViewBag.Routes = await _routeService.GetRoutesAsync();

            return View(eventModel);
        }

        // Submit the updated Event
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(int id, EventViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Routes = await _routeService.GetRoutesAsync();
                return View(model);
            }

            var response = await _eventService.UpdateEventAsync(id, model);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("ManageEvents");
            }

            ModelState.AddModelError(
                "",
                "Unable to update event. Please check your details.");

            ViewBag.Routes = await _routeService.GetRoutesAsync();

            return View(model);
        }

        // Delete an Event
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Account");
            }

            var response = await _eventService.DeleteEventAsync(id);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("ManageEvents");
            }

            TempData["ErrorMessage"] =
                "Unable to delete this event. It may have existing enrolments.";

            return RedirectToAction("ManageEvents");
        }

        // Display events for category management
        public async Task<IActionResult> ManageCategories()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var events = await _eventService.GetEventsAsync();
                return View(events);
            }
            catch
            {
                ViewBag.ErrorMessage =
                    "Unable to load events. Please try again later.";

                return View(new List<EventViewModel>());
            }
        }

        // Display categories for a selected event
        public async Task<IActionResult> EventCategories(int eventId)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
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
                ViewBag.EventID = eventId;

                ViewBag.ErrorMessage =
                    "Unable to load event categories. Please try again later.";

                return View(new List<CategoryViewModel>());
            }
        }

        // Display the Create Category form
        public IActionResult CreateCategory(int eventId)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new CategoryViewModel
            {
                EventID = eventId
            };

            return View(model);
        }

        // Submit the Create Category form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(CategoryViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response = await _categoryService.CreateCategoryAsync(model);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction(
                    "EventCategories",
                    new { eventId = model.EventID });
            }

            ModelState.AddModelError(
                "",
                "Unable to create category. Please check your details.");

            return View(model);
        }

        // Display the Edit Category form
        public async Task<IActionResult> EditCategory(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Account");
            }

            var categories = await _categoryService.GetCategoriesAsync();

            var category = categories.FirstOrDefault(
                c => c.CategoryID == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // Submit the updated Category
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(
            int id, CategoryViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response = await _categoryService.UpdateCategoryAsync(id, model);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction(
                    "EventCategories",
                    new { eventId = model.EventID });
            }

            ModelState.AddModelError(
                "",
                "Unable to update category. Please check your details.");

            return View(model);
        }
        // Delete an Event Category
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id, int eventId)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Account");
            }

            var response = await _categoryService.DeleteCategoryAsync(id);

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] =
                    "Unable to delete category. It may have existing enrolments.";
            }

            return RedirectToAction(
                "EventCategories",
                new { eventId = eventId });
        }
    }
}