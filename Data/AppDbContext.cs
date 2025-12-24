using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RestoBooking.Models;

namespace RestoBooking.Data
{
    public class AppDbContext : IdentityDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Table> Tables { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<MenuCategory> MenuCategories { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<ReservationMenuItem> ReservationMenuItems { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Table>()
                .Property(t => t.BasePricePerPerson)
                .HasPrecision(18, 2);

            builder.Entity<MenuItem>()
                .Property(m => m.Price)
                .HasPrecision(18, 2);

            builder.Entity<Reservation>()
                .Property(r => r.TotalPrice)
                .HasPrecision(18, 2);
        }
    }
}
