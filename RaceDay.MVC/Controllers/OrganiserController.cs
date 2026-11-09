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
        private readonly EntryService _entryService;
        private readonly ResultService _resultService;
        private readonly ImageService _imageService;
        public OrganiserController(
            EventService eventService,
            RouteService routeService,
            CategoryService categoryService,
            EntryService entryService,
            ResultService resultService,
            ImageService imageService)
        {
            _eventService = eventService;
            _routeService = routeService;
            _categoryService = categoryService;
            _entryService = entryService;
            _resultService = resultService;
            _imageService = imageService;
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
        public async Task<IActionResult> CreateEvent(
         EventViewModel model,
         IFormFile? imageFile)
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

            // Upload the event image to Azure Blob Storage if one was selected.
            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    model.ImageUrl = await _imageService.UploadImageAsync(imageFile);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                    ViewBag.Routes = await _routeService.GetRoutesAsync();
                    return View(model);
                }
            }

            // Send the event details, including ImageUrl, to the API.
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
        public async Task<IActionResult> EditEvent(int id, EventViewModel model,IFormFile? imageFile)
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
            // Preserve the existing image unless a replacement is uploaded.
            var existingEvent = await _eventService.GetEventForEditAsync(id);

            if (existingEvent == null)
            {
                return NotFound();
            }

            model.ImageUrl = existingEvent.ImageUrl;

            // Upload a replacement image if the organiser selected one.
            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    model.ImageUrl = await _imageService.UploadImageAsync(imageFile);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);

                    ViewBag.Routes = await _routeService.GetRoutesAsync();

                    return View(model);
                }
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
        public async Task<IActionResult> ParticipantEnrolments()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            try
            {
                var entries = await _entryService.GetOrganiserEntriesAsync();

                return View(entries);
            }
            catch
            {
                ViewBag.ErrorMessage =
                    "Unable to load participant enrolments. Please try again later.";

                return View(new List<OrganiserEntryViewModel>());
            }
        }
        // Display results belonging to the organiser's events
        public async Task<IActionResult> ManageResults()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            try
            {
                var model = new ManageResultsViewModel
                {
                    Results = await _resultService.GetMyResultsAsync(),
                    Entries = await _entryService.GetOrganiserEntriesAsync()
                };

                return View(model);
            }
            catch
            {
                ViewBag.ErrorMessage =
                    "Unable to load race results. Please try again later.";

                return View(new ManageResultsViewModel());
            }
        }
        // Display the form for recording a race result
        public async Task<IActionResult> CreateResult()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var entries = await _entryService.GetOrganiserEntriesAsync();

            return View(entries);
        }

        // Save a new race result through the API
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateResult(ResultViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                var entries = await _entryService.GetOrganiserEntriesAsync();
                ViewBag.ErrorMessage = "Please check the result details.";
                return View(entries);
            }

            try
            {
                var response = await _resultService.CreateResultAsync(model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Race result recorded successfully.";
                    return RedirectToAction(nameof(ManageResults));
                }

                ViewBag.ErrorMessage =
                    "Unable to record the result. Check that the participant enrolment is valid.";
            }
            catch
            {
                ViewBag.ErrorMessage =
                    "An error occurred while recording the race result.";
            }

            var organiserEntries = await _entryService.GetOrganiserEntriesAsync();
            return View(organiserEntries);
        }
        // Display the form for editing an existing race result
        public async Task<IActionResult> EditResult(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            try
            {
                var result = await _resultService.GetResultByIdAsync(id);

                if (result == null)
                    return NotFound();

                var model = new EditResultViewModel
                {
                    Result = result,
                    Entries = await _entryService.GetOrganiserEntriesAsync()
                };

                return View(model);
            }
            catch
            {
                TempData["ErrorMessage"] = "Unable to load the race result.";
                return RedirectToAction(nameof(ManageResults));
            }
        }

        // Save changes to an existing race result
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditResult(
            int id, EditResultViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                model.Entries = await _entryService.GetOrganiserEntriesAsync();
                return View(model);
            }

            try
            {
                var response = await _resultService.UpdateResultAsync(
                    id, model.Result);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Race result updated successfully.";
                    return RedirectToAction(nameof(ManageResults));
                }

                ViewBag.ErrorMessage =
                    "Unable to update the result. Please check the details.";
            }
            catch
            {
                ViewBag.ErrorMessage =
                    "An error occurred while updating the race result.";
            }

            model.Entries = await _entryService.GetOrganiserEntriesAsync();
            return View(model);
        }
        // Delete an existing race result
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteResult(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            try
            {
                var response = await _resultService.DeleteResultAsync(id);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Race result deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] =
                        "Unable to delete the race result.";
                }
            }
            catch
            {
                TempData["ErrorMessage"] =
                    "An error occurred while deleting the race result.";
            }

            return RedirectToAction(nameof(ManageResults));
        }
    }
}