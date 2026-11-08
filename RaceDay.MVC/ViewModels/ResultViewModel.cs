using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.ViewModels
{
    public class ResultViewModel
    {
        public int ResultID { get; set; }

        [Required]
        public int EntryID { get; set; }

        [Required]
        public TimeSpan FinishTime { get; set; }

        [Range(1, 100000)]
        public int Position { get; set; }

        [Range(typeof(decimal), "0.01", "1000")]
        public decimal AveragePace { get; set; }

        public DateTime ResultDate { get; set; } = DateTime.Now;
    }
}