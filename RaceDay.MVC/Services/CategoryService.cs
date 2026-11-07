using System.Net.Http.Json;
using RaceDay.MVC.ViewModels;

namespace RaceDay.MVC.Services
{
    public class CategoryService
    {
        private readonly ApiService _apiService;

        public CategoryService(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<List<CategoryViewModel>> GetCategoriesAsync()
        {
            var client = _apiService.GetAuthenticatedClient();

            var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>(
                "api/EventCategories");

            return categories ?? new List<CategoryViewModel>();
        }
        public async Task<List<CategoryViewModel>> GetCategoriesByEventAsync(int eventId)
        {
            var categories = await GetCategoriesAsync();

            return categories
                .Where(c => c.EventID == eventId)
                .ToList();
        }
    }
}