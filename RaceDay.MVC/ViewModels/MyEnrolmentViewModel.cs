namespace RaceDay.MVC.ViewModels
{
    public class MyEnrolmentViewModel
    {
        public int EntryID { get; set; }
        public int ParticipantID { get; set; }
        public int CategoryID { get; set; }
        public DateTime EntryDate { get; set; }
        public string EntryStatus { get; set; } = string.Empty;
        public int? RaceNumber { get; set; }

        public CategoryInfo? Category { get; set; }
    }

    public class CategoryInfo
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal DistanceKM { get; set; }
        public decimal EntryFee { get; set; }

        public EventInfo? Event { get; set; }
    }

    public class EventInfo
    {
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Venue { get; set; } = string.Empty;
    }
}