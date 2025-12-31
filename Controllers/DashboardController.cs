using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestoBooking.Data;
using RestoBooking.Models.ViewModels;

namespace RestoBooking.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new DashboardViewModel();

            // 🔹 Plats les plus commandés
            var dishes = await _context.ReservationMenuItems
                .Include(rm => rm.MenuItem)
                .GroupBy(rm => rm.MenuItem.Name)
                .Select(g => new
                {
                    Label = g.Key,
                    Count = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            vm.TopDishesLabels = dishes.Select(d => d.Label).ToList();
            vm.TopDishesValues = dishes.Select(d => d.Count).ToList();

            // 🔹 Tables les plus réservées
            var tables = await _context.Reservations
                .Include(r => r.Table)
                .GroupBy(r => r.Table!.Name)
                .Select(g => new
                {
                    Label = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            vm.TopTablesLabels = tables.Select(t => t.Label).ToList();
            vm.TopTablesValues = tables.Select(t => t.Count).ToList();

            return View(vm);
        }
    }
}
