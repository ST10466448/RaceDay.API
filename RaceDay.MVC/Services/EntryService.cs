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

        // Participant enrols in an event category
        public async Task<HttpResponseMessage> EnrolAsync(
            EntryViewModel model)
        {
            var client = _apiService.GetAuthenticatedClient();

            return await client.PostAsJsonAsync(
                "api/Entries", model);
        }

        // Get enrolments for the logged-in participant
        public async Task<List<MyEnrolmentViewModel>> GetMyEnrolmentsAsync()
        {
            var client = _apiService.GetAuthenticatedClient();

            var enrolments =
                await client.GetFromJsonAsync<List<MyEnrolmentViewModel>>(
                    "api/Entries/my");

            return enrolments ?? new List<MyEnrolmentViewModel>();
        }

        // Get enrolments for the logged-in organiser
        public async Task<List<OrganiserEntryViewModel>> GetOrganiserEntriesAsync()
        {
            var client = _apiService.GetAuthenticatedClient();

            var entries =
                await client.GetFromJsonAsync<List<OrganiserEntryViewModel>>(
                    "api/Entries");

            return entries ?? new List<OrganiserEntryViewModel>();
        }
    }
}