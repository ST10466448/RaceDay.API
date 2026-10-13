using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models
{
    public class Route
    {
        public int RouteID { get; set; }

        [Required]
        [MaxLength(100)]
        public string RouteName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, 1000)]
        public decimal DistanceKM { get; set; }

        [Required]
        [MaxLength(200)]
        public string StartPoint { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string FinishPoint { get; set; } = string.Empty;

        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}