using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RestoBooking.Models.ViewModels
{
    public class ProfileReservationViewModel
    {
        public int Id { get; set; }
        public DateTime ReservationDate { get; set; }
        public string TableName { get; set; } = string.Empty;
        public string TableCategoryLabel { get; set; } = string.Empty;
        public int NumberOfPeople { get; set; }
        public string OccasionLabel { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public decimal TablePricePerPerson { get; set; }
        public decimal OccasionPricePerPerson { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsUpcoming { get; set; }
        public bool IsCancelled { get; set; }
        public DateTime? CancelledAt { get; set; }
        public decimal CancellationFee { get; set; }
        public decimal RefundAmount { get; set; }
    }

    public class ProfileViewModel
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        [Display(Name = "Mes réservations")]
        public List<ProfileReservationViewModel> Reservations { get; set; } = new();
    }
}