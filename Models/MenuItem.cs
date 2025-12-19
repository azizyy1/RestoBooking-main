namespace RestoBooking.Models
{
    public class MenuItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; } = true;

        public int MenuCategoryId { get; set; }
        public MenuCategory? Category { get; set; }
    }
}
