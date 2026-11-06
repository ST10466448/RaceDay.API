using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using RaceDay.MVC.ViewModels;

namespace RaceDay.MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;

        public AccountController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var response = await _authService.LoginAsync(model);

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(string.Empty,
                        "Invalid email or password.");
                    return View(model);
                }

                var result = await response.Content.ReadFromJsonAsync<JsonElement>();

                var token = result.GetProperty("token").GetString();
                var user = result.GetProperty("user");

                HttpContext.Session.SetString("JWToken", token!);
                HttpContext.Session.SetString("UserID",
                    user.GetProperty("userID").ToString());
                HttpContext.Session.SetString("FullName",
                    user.GetProperty("fullName").GetString()!);
                HttpContext.Session.SetString("Email",
                    user.GetProperty("email").GetString()!);
                HttpContext.Session.SetString("Role",
                    user.GetProperty("role").GetString()!);

                if (HttpContext.Session.GetString("Role") == "Organiser")
                    return RedirectToAction("Dashboard", "Organiser");

                return RedirectToAction("Index", "Participant");
            }
            catch
            {
                ModelState.AddModelError(string.Empty,
                    "Unable to connect to the RaceDay service. Please try again.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var response = await _authService.RegisterAsync(model);

                if (!response.IsSuccessStatusCode)
                {
                    var apiError = await response.Content.ReadAsStringAsync();

                    ModelState.AddModelError(string.Empty,
                        $"Registration failed. API response: {apiError}");

                    return View(model);
                }

                TempData["SuccessMessage"] =
                    "Registration successful! Please log in.";

                return RedirectToAction("Login");
            }
            catch
            {
                ModelState.AddModelError(string.Empty,
                    "Unable to connect to the RaceDay service. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}