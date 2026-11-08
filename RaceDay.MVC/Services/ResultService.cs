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

        public async Task<List<ResultViewModel>> GetMyResultsAsync()
        {
            var client = _apiService.GetAuthenticatedClient();

            var results = await client.GetFromJsonAsync<List<ResultViewModel>>(
                "api/Results");

            return results ?? new List<ResultViewModel>();
        }
    }
}