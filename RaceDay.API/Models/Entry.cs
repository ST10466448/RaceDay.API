using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models
{
    public class Entry
    {
        public int EntryID { get; set; }

        [Required]
        public int ParticipantID { get; set; }

        [Required]
        public int CategoryID { get; set; }

        public DateTime EntryDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(20)]
        public string EntryStatus { get; set; } = "Confirmed";

        public int? RaceNumber { get; set; }

        public User? Participant { get; set; }

        public EventCategory? Category { get; set; }

        public Result? Result { get; set; }
    }
}