using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using RaceDay.MVC.ViewModels;

namespace RaceDay.MVC.Controllers
{
    public class ParticipantController : Controller
    {
        private readonly EventService _eventService;
        private readonly CategoryService _categoryService;
        private readonly EntryService _entryService;
        private readonly ResultService _resultService;

        public ParticipantController(
            EventService eventService,
            CategoryService categoryService,
            EntryService entryService,
            ResultService resultService             )
        {
            _eventService = eventService;
            _categoryService = categoryService;
            _entryService = entryService;
            _resultService = resultService;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enrol(
            RaceDay.MVC.ViewModels.EntryViewModel model,
            int eventId)
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please select a valid category.";
                return RedirectToAction("Categories", new { eventId });
            }

            try
            {
                var response = await _entryService.EnrolAsync(model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "You have successfully enrolled in this category!";
                }
                else
                {
                    TempData["ErrorMessage"] =
                        "Enrolment failed. Please check your selection and try again.";
                }
            }
            catch
            {
                TempData["ErrorMessage"] =
                    "Unable to process your enrolment. Please try again.";
            }

            return RedirectToAction("Categories", new { eventId });
        }

        public async Task<IActionResult> MyEnrolments()
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var enrolments = await _entryService.GetMyEnrolmentsAsync();

                return View(enrolments);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;

                return View(
                    new List<RaceDay.MVC.ViewModels.MyEnrolmentViewModel>());
            }
        }
        public async Task<IActionResult> MyResults()
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var results = await _resultService.GetMyResultsAsync();

                return View(results);
            }
            catch
            {
                ViewBag.ErrorMessage =
                    "Unable to load your results. Please try again later.";

                return View(new List<ResultViewModel>());
            }
        }
    }
}