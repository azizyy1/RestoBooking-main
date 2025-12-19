using System.ComponentModel.DataAnnotations;

namespace RestoBooking.Models
{
    public class ReservationMenuItem
    {
        public int Id { get; set; }

        public int ReservationId { get; set; }
        public Reservation Reservation { get; set; } = null!;

        public int MenuItemId { get; set; }
        public MenuItem MenuItem { get; set; } = null!;

        public int Quantity { get; set; }
    }
}
