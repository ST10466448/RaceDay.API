namespace RaceDay.MVC.ViewModels
{
    public class RouteViewModel
    {
        public int RouteID { get; set; }

        public string RouteName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal DistanceKM { get; set; }

        public string StartPoint { get; set; } = string.Empty;

        public string FinishPoint { get; set; } = string.Empty;
    }
}