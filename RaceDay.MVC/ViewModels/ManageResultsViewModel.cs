namespace RaceDay.MVC.ViewModels
{
    public class ManageResultsViewModel
    {
        public List<ResultViewModel> Results { get; set; } = new();

        public List<OrganiserEntryViewModel> Entries { get; set; } = new();
    }
}