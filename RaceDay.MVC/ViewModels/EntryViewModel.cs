using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.ViewModels
{
    public class EntryViewModel
    {
        [Required]
        public int CategoryID { get; set; }

        public int? RaceNumber { get; set; }
    }
}