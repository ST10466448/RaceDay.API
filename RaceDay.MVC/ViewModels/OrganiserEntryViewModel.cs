namespace RaceDay.MVC.ViewModels
{
    public class OrganiserEntryViewModel
    {
        public int EntryID { get; set; }

        public int ParticipantID { get; set; }

        public string ParticipantName { get; set; } = string.Empty;

        public string ParticipantEmail { get; set; } = string.Empty;

        public int CategoryID { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public string EventName { get; set; } = string.Empty;

        public decimal DistanceKM { get; set; }

        public decimal EntryFee { get; set; }

        public DateTime EntryDate { get; set; }

        public string EntryStatus { get; set; } = string.Empty;

        public int? RaceNumber { get; set; }
    }
}