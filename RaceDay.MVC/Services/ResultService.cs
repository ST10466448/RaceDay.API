using System.Net.Http.Json;
using RaceDay.MVC.ViewModels;

namespace RaceDay.MVC.Services
{
    public class ResultService
    {
        private readonly ApiService _apiService;

        public ResultService(ApiService apiService)
        {
            _apiService = apiService;
        }

        // Participants and organisers retrieve their permitted results.
        public async Task<List<ResultViewModel>> GetMyResultsAsync()
        {
            var client = _apiService.GetAuthenticatedClient();

            var results = await client.GetFromJsonAsync<List<ResultViewModel>>(
                "api/Results");

            return results ?? new List<ResultViewModel>();
        }

        // Organisers create a race result.
        public async Task<HttpResponseMessage> CreateResultAsync(
            ResultViewModel model)
        {
            var client = _apiService.GetAuthenticatedClient();

            return await client.PostAsJsonAsync("api/Results", model);
        }

        // Organisers retrieve a result for editing.
        public async Task<ResultViewModel?> GetResultByIdAsync(int id)
        {
            var client = _apiService.GetAuthenticatedClient();

            return await client.GetFromJsonAsync<ResultViewModel>(
                $"api/Results/{id}");
        }

        // Organisers update an existing result.
        public async Task<HttpResponseMessage> UpdateResultAsync(
            int id, ResultViewModel model)
        {
            var client = _apiService.GetAuthenticatedClient();

            return await client.PutAsJsonAsync(
                $"api/Results/{id}", model);
        }

        // Organisers delete a result.
        public async Task<HttpResponseMessage> DeleteResultAsync(int id)
        {
            var client = _apiService.GetAuthenticatedClient();

            return await client.DeleteAsync($"api/Results/{id}");
        }
    }
}