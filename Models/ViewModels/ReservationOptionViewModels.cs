using System.ComponentModel.DataAnnotations;

namespace RestoBooking.Models.ViewModels;

public class TableOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string CategoryLabel { get; set; } = null!;
    public TableCategory Category { get; set; }
    public decimal BasePricePerPerson { get; set; }
    public decimal PricePerPerson { get; set; }
}

public class OccasionOptionViewModel
{
    public OccasionType Value { get; set; }
    public string Label { get; set; } = null!;
    public decimal PricePerPerson { get; set; }
}