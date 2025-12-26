using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestoBooking.Models
{
    // 🔹 Types d’occasion possibles
    public enum OccasionType
    {
        Standard = 0,
        [Display(Name = "Occasions personnelles & familiales")]
        OccasionsPersonnellesFamiliales = 1,

        [Display(Name = "Célébrations et fêtes")]
        CelebrationsFetes = 2,

        [Display(Name = "Occasions professionnelles")]
        OccasionsProfessionnelles = 3,

        [Display(Name = "Événements scolaires & universitaires")]
        EvenementsScolairesUniversitaires = 4,

        [Display(Name = "Événements touristiques & culturels")]
        EvenementsTouristiquesCulturels = 5,

        [Display(Name = "Réservation VIP")]
        ReservationVIP = 6
    }

    public class Reservation
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Nom du client")]
        public string CustomerName { get; set; } = null!;

        [Required, EmailAddress]
        [Display(Name = "Email")]
        public string CustomerEmail { get; set; } = null!;

        [Required]
        [Display(Name = "Téléphone")]
        public string CustomerPhone { get; set; } = null!;

        [Required]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime ReservationDate { get; set; }

        // ⏰ Heure séparée, non stockée directement en BD
        [NotMapped]
        [Display(Name = "Heure")]
        [DataType(DataType.Time)]
        public TimeSpan? ReservationTime { get; set; }

        [Required]
        [Range(1, 20)]
        [Display(Name = "Nombre de personnes")]
        public int NumberOfPeople { get; set; }

        // 🔗 Table
        [Required]
        [Display(Name = "Table")]
        public int TableId { get; set; }

        public Table? Table { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        // 🔥 Occasion (standard, anniv, etc.)
        [Display(Name = "Occasion")]
        public OccasionType Occasion { get; set; } = OccasionType.Standard;

        // 🔥 Prix total (calculé côté serveur)
        [Display(Name = "Prix total")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        [Display(Name = "Annulée")]
        public bool IsCancelled { get; set; }

        [Display(Name = "Date d'annulation")]
        public DateTime? CancelledAt { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Frais d'annulation")]
        public decimal CancellationFee { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Montant remboursé")]
        public decimal RefundAmount { get; set; }

    }
}
