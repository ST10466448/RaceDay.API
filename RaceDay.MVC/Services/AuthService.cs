using System.Net.Http.Json;
using RaceDay.MVC.ViewModels;

namespace RaceDay.MVC.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;

        public AuthService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("RaceDayApi");
        }

        public async Task<HttpResponseMessage> LoginAsync(
            LoginViewModel model)
        {
            var request = new
            {
                Email = model.Email,
                Password = model.Password
            };

            return await _httpClient.PostAsJsonAsync(
                "api/Auth/login", request);
        }

        public async Task<HttpResponseMessage> RegisterAsync(
            RegisterViewModel model)
        {
            var request = new
            {
                FullName = model.FullName,
                Email = model.Email,
                Password = model.Password,
                Role = "Participant"
            };

            return await _httpClient.PostAsJsonAsync(
                "api/Auth/register", request);
        }
    }
}