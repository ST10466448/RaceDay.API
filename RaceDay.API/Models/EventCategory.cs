using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models
{
    public class EventCategory
    {
        public int CategoryID { get; set; }

        [Required]
        public int EventID { get; set; }

        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [Range(0.01, 1000)]
        public decimal DistanceKM { get; set; }

        [Range(0, 100000)]
        public decimal EntryFee { get; set; }

        [Range(1, 100000)]
        public int MaxParticipants { get; set; }

        public Event? Event { get; set; }

        public ICollection<Entry> Entries { get; set; }
            = new List<Entry>();
    }
}