using System;
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

            vm.TotalTables = await _context.Tables.CountAsync();
            vm.TotalMenuItems = await _context.MenuItems.CountAsync();
            vm.TotalReservations = await _context.Reservations.CountAsync(r => !r.IsCancelled);

            var today = DateTime.Today;
            vm.TodayReservations = await _context.Reservations
                .CountAsync(r => !r.IsCancelled && r.ReservationDate.Date == today);

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

            var startDate = new DateTime(today.Year, today.Month, 1).AddMonths(-5);
            var reservationTrends = await _context.Reservations
                .Where(r => !r.IsCancelled && r.ReservationDate >= startDate)
                .GroupBy(r => new { r.ReservationDate.Year, r.ReservationDate.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month)
                .ToListAsync();

            vm.ReservationTrendsLabels = reservationTrends
                .Select(rt => new DateTime(rt.Year, rt.Month, 1).ToString("MMM yy"))
                .ToList();

            vm.ReservationTrendsValues = reservationTrends
                .Select(rt => rt.Count)
                .ToList();

            return View(vm);
        }
    }
}
