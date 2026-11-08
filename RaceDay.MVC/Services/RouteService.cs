using System.Net.Http.Json;
using RaceDay.MVC.ViewModels;

namespace RaceDay.MVC.Services
{
    public class RouteService
    {
        private readonly ApiService _apiService;

        public RouteService(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<List<RouteViewModel>> GetRoutesAsync()
        {
            var client = _apiService.GetAuthenticatedClient();

            var routes = await client.GetFromJsonAsync<List<RouteViewModel>>(
                "api/Routes");

            return routes ?? new List<RouteViewModel>();
        }
    }
}