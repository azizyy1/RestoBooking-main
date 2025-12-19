using System.Collections.Generic;

namespace RestoBooking.Models
{
    public class MenuCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        public ICollection<MenuItem> Items { get; set; } = new List<MenuItem>();
    }
}
