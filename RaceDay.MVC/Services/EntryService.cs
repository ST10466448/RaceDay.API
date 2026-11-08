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
        public async Task<List<MyEnrolmentViewModel>> GetMyEnrolmentsAsync()
        {
            var client = _apiService.GetAuthenticatedClient();

            var enrolments = await client.GetFromJsonAsync<List<MyEnrolmentViewModel>>(
                "api/Entries/my");

            return enrolments ?? new List<MyEnrolmentViewModel>();
        }
    }
}