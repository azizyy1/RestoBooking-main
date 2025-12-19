using System.Collections.Generic;

namespace RestoBooking.Models.ViewModels
{
    public class DashboardViewModel
    {
        public List<string> TopDishesLabels { get; set; } = new();
        public List<int> TopDishesValues { get; set; } = new();

        public List<string> TopTablesLabels { get; set; } = new();
        public List<int> TopTablesValues { get; set; } = new();
    }
}
