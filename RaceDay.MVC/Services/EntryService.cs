using System.Net.Http.Json;
using RaceDay.MVC.ViewModels;

namespace RaceDay.MVC.Services
{
    public class EntryService
    {
        private readonly ApiService _apiService;

        public EntryService(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<HttpResponseMessage> EnrolAsync(
            EntryViewModel model)
        {
            var client = _apiService.GetAuthenticatedClient();

            return await client.PostAsJsonAsync(
                "api/Entries", model);
        }
    }
}