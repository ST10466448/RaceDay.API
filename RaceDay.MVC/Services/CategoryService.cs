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

        // Retrieve all categories
        public async Task<List<CategoryViewModel>> GetCategoriesAsync()
        {
            var client = _apiService.GetAuthenticatedClient();

            var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>(
                "api/EventCategories");

            return categories ?? new List<CategoryViewModel>();
        }

        // Retrieve categories belonging to a specific event
        public async Task<List<CategoryViewModel>> GetCategoriesByEventAsync(int eventId)
        {
            var categories = await GetCategoriesAsync();

            return categories
                .Where(c => c.EventID == eventId)
                .ToList();
        }

        // Create a category
        public async Task<HttpResponseMessage> CreateCategoryAsync(
            CategoryViewModel model)
        {
            var client = _apiService.GetAuthenticatedClient();

            return await client.PostAsJsonAsync(
                "api/EventCategories", model);
        }

        // Update a category
        public async Task<HttpResponseMessage> UpdateCategoryAsync(
            int id, CategoryViewModel model)
        {
            var client = _apiService.GetAuthenticatedClient();

            return await client.PutAsJsonAsync(
                $"api/EventCategories/{id}", model);
        }

        // Delete a category
        public async Task<HttpResponseMessage> DeleteCategoryAsync(int id)
        {
            var client = _apiService.GetAuthenticatedClient();

            return await client.DeleteAsync(
                $"api/EventCategories/{id}");
        }
    }
}