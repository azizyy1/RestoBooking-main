using System.ComponentModel.DataAnnotations;

namespace RestoBooking.Models
{
    public enum TableCategory
    {
        [Display(Name = "Table standard")]
        Standard = 0,
         [Display(Name = "Table VIP & exclusive")]
        VIPExclusive = 1,

        [Display(Name = "Espace privé")]
        EspacePrive = 2,

        [Display(Name = "Table d'expérience gastronomique")]
        ExperienceGastronomique = 3,
        [Display(Name = "Table avec emplacement premium")]
        EmplacementPremium = 4,

        [Display(Name = "Table business haut de gamme")]
        BusinessHautDeGamme = 5,

        [Display(Name = "Table événementielle")]
        Evenementielle = 6
    }

    public class Table
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int Capacity { get; set; }
        public bool IsActive { get; set; }

        // 🔥 Nouveau : type de table
        public TableCategory Category { get; set; } = TableCategory.Standard;
         // 🔥 Option : prix de base par personne pour cette table
        public decimal BasePricePerPerson { get; set; } = 120m; // tarif de base personnalisé
    }
}
