using System.Net.Http.Json;
using RaceDay.MVC.ViewModels;


namespace RaceDay.MVC.Services
{
    public class EventService
    {
        private readonly ApiService _apiService;

        public EventService(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<List<EventViewModel>> GetEventsAsync()
        {
            var client = _apiService.GetAuthenticatedClient();

            var events = await client.GetFromJsonAsync<List<EventViewModel>>(
                "api/Events");

            return events ?? new List<EventViewModel>();
        }
        public async Task<EventDetailsViewModel?> GetEventByIdAsync(int id)
        {
            var client = _apiService.GetAuthenticatedClient();

            return await client.GetFromJsonAsync<EventDetailsViewModel>(
                $"api/Events/{id}");
        }
    }
}