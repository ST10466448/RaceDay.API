using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using RaceDay.MVC.ViewModels;

namespace RaceDay.MVC.Controllers
{
    public class OrganiserController : Controller
    {
        private readonly EventService _eventService;
        private readonly RouteService _routeService;

        public OrganiserController(
            EventService eventService,
            RouteService routeService)
        {
            _eventService = eventService;
            _routeService = routeService;
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

            var routes = await _routeService.GetRoutesAsync();

            ViewBag.Routes = routes;

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

            ModelState.AddModelError("", "Unable to create event. Please check your details.");

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
    }
}