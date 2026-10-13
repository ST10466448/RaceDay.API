using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models
{
    public class Event
    {
        public int EventID { get; set; }

        [Required]
        public int OrganizerID { get; set; }

        [Required]
        public int RouteID { get; set; }

        [Required]
        [MaxLength(150)]
        public string EventName { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public TimeSpan StartTime { get; set; }

        [Required]
        [MaxLength(200)]
        public string Venue { get; set; } = string.Empty;

        [MaxLength(150)]
        public string WeatherLocation { get; set; } = string.Empty;

        public User? Organizer { get; set; }

        public Route? Route { get; set; }

        public ICollection<EventCategory> Categories { get; set; }
            = new List<EventCategory>();
    }
}