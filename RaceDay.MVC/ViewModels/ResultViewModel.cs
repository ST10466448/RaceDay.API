namespace RaceDay.MVC.ViewModels
{
    public class ResultViewModel
    {
        public int ResultID { get; set; }

        public int EntryID { get; set; }

        public TimeSpan FinishTime { get; set; }

        public int Position { get; set; }

        public decimal AveragePace { get; set; }

        public DateTime ResultDate { get; set; }
    }
}