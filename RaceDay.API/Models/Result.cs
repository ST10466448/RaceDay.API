using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models
{
    public class Result
    {
        public int ResultID { get; set; }

        [Required]
        public int EntryID { get; set; }

        public TimeSpan FinishTime { get; set; }

        [Range(1, 100000)]
        public int Position { get; set; }

        [Range(typeof(decimal), "0.01", "1000")]
        public decimal AveragePace { get; set; }

        public DateTime ResultDate { get; set; } = DateTime.UtcNow;

        public Entry? Entry { get; set; }
    }
}