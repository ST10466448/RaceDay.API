
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

        // Retrieve all events
        public async Task<List<EventViewModel>> GetEventsAsync()
        {
            var client = _apiService.GetAuthenticatedClient();

            var events = await client.GetFromJsonAsync<List<EventViewModel>>(
                "api/Events");

            return events ?? new List<EventViewModel>();
        }

        // Retrieve an individual event
        public async Task<EventDetailsViewModel?> GetEventByIdAsync(int id)
        {
            var client = _apiService.GetAuthenticatedClient();

            return await client.GetFromJsonAsync<EventDetailsViewModel>(
                $"api/Events/{id}");
        }
        public async Task<EventViewModel?> GetEventForEditAsync(int id)
        {
            var client = _apiService.GetAuthenticatedClient();

            return await client.GetFromJsonAsync<EventViewModel>(
                $"api/Events/{id}");
        }

        // Create an event
        public async Task<HttpResponseMessage> CreateEventAsync(
            EventViewModel model)
        {
            var client = _apiService.GetAuthenticatedClient();

            return await client.PostAsJsonAsync(
                "api/Events", model);
        }

        // Update an event
        public async Task<HttpResponseMessage> UpdateEventAsync(
            int id, EventViewModel model)
        {
            var client = _apiService.GetAuthenticatedClient();

            return await client.PutAsJsonAsync(
                $"api/Events/{id}", model);
        }

        // Delete an event
        public async Task<HttpResponseMessage> DeleteEventAsync(int id)
        {
            var client = _apiService.GetAuthenticatedClient();

            return await client.DeleteAsync($"api/Events/{id}");
        }
    }
}