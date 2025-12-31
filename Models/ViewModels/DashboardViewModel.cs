using System.Collections.Generic;

namespace RestoBooking.Models.ViewModels
{
    public class DashboardViewModel
    {
        public List<string> TopDishesLabels { get; set; } = new();
        public List<int> TopDishesValues { get; set; } = new();

        public List<string> TopTablesLabels { get; set; } = new();
        public List<int> TopTablesValues { get; set; } = new();
        public int TotalTables { get; set; }
        public int TotalMenuItems { get; set; }
        public int TotalReservations { get; set; }
        public int TodayReservations { get; set; }

        public List<string> ReservationTrendsLabels { get; set; } = new();
        public List<int> ReservationTrendsValues { get; set; } = new();
    }
}
