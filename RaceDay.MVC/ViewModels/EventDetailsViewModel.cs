namespace RaceDay.MVC.ViewModels
{
    public class EventDetailsViewModel
    {
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public string Venue { get; set; } = string.Empty;
        public int RouteID { get; set; }
    }
}