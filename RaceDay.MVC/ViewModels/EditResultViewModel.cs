
namespace RaceDay.MVC.ViewModels
{
    public class EditResultViewModel
    {
        public ResultViewModel Result { get; set; } = new();

        public List<OrganiserEntryViewModel> Entries { get; set; } = new();
    }
}