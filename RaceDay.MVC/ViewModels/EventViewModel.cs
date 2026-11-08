using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.ViewModels
{
    public class EventViewModel
    {
        public int EventID { get; set; }

        [Required]
        [StringLength(150)]
        public string EventName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(Run|Walk|Cycle)$",
            ErrorMessage = "Event type must be Run, Walk or Cycle.")]
        public string EventType { get; set; } = string.Empty;

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        [StringLength(200)]
        public string Venue { get; set; } = string.Empty;

        [StringLength(150)]
        public string WeatherLocation { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int RouteID { get; set; }
    }
}