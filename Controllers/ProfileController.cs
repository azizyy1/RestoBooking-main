using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestoBooking.Data;
using RestoBooking.Models;
using RestoBooking.Models.ViewModels;
using RestoBooking.Services;
using System.ComponentModel.DataAnnotations;

namespace RestoBooking.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;

        private static readonly Dictionary<TableCategory, decimal> TableCategoryMultipliers = new()
        {
            { TableCategory.Standard, 1.35m },
            { TableCategory.VIPExclusive, 2.8m },
            { TableCategory.EspacePrive, 2.4m },
            { TableCategory.ExperienceGastronomique, 3.25m },
            { TableCategory.EmplacementPremium, 2.3m },
            { TableCategory.BusinessHautDeGamme, 2.6m },
            { TableCategory.Evenementielle, 2.45m }
        };

        private static readonly Dictionary<OccasionType, decimal> OccasionPricesPerPerson = new()
        {
            { OccasionType.Standard, 0m },
            { OccasionType.OccasionsPersonnellesFamiliales, 120m },
            { OccasionType.CelebrationsFetes, 170m },
            { OccasionType.OccasionsProfessionnelles, 220m },
            { OccasionType.EvenementsScolairesUniversitaires, 130m },
            { OccasionType.EvenementsTouristiquesCulturels, 185m },
            { OccasionType.ReservationVIP, 320m }
        };

            public ProfileController(AppDbContext context, UserManager<IdentityUser> userManager, IEmailService emailService)        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var userEmail = user.Email?.Trim();
            var reservations = await _context.Reservations
                .Include(r => r.Table)
                .Where(r => userEmail != null && r.CustomerEmail.ToLower() == userEmail.ToLower())
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();

            var model = new ProfileViewModel
            {
                DisplayName = FormatDisplayName(user),
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Reservations = reservations.Select(r =>
                {
                    var tablePricePerPerson = r.Table != null
                        ? Math.Round(r.Table.BasePricePerPerson * TableCategoryMultipliers[r.Table.Category], 2, MidpointRounding.AwayFromZero)
                        : 0m;

                    var occasionPricePerPerson = GetOccasionPrice(r.Occasion);
                    var calculatedTotal = Math.Round(r.NumberOfPeople * (tablePricePerPerson + occasionPricePerPerson), 2, MidpointRounding.AwayFromZero);
                    var totalPrice = r.TotalPrice > 0 ? r.TotalPrice : calculatedTotal;

                    var cancellationFee = r.IsCancelled && r.CancellationFee > 0
                        ? r.CancellationFee
                        : r.IsCancelled
                            ? Math.Round(totalPrice * 0.05m, 2, MidpointRounding.AwayFromZero)
                            : 0m;

                    var refundAmount = r.IsCancelled && r.RefundAmount > 0
                        ? r.RefundAmount
                        : r.IsCancelled
                            ? Math.Max(totalPrice - cancellationFee, 0)
                            : 0m;


                    return new ProfileReservationViewModel
                    {
                        Id = r.Id,
                        ReservationDate = r.ReservationDate,
                        TableName = r.Table?.Name ?? "Table inconnue",
                        TableCategoryLabel = r.Table != null ? GetDisplayName(r.Table.Category) : "N/A",
                        NumberOfPeople = r.NumberOfPeople,
                        OccasionLabel = GetDisplayName(r.Occasion),
                        Notes = r.Notes,
                        TablePricePerPerson = tablePricePerPerson,
                        OccasionPricePerPerson = occasionPricePerPerson,
                        TotalPrice = r.TotalPrice,
                        IsUpcoming = r.ReservationDate >= DateTime.Now,
                        IsCancelled = r.IsCancelled,
                        CancelledAt = r.CancelledAt,
                        CancellationFee = cancellationFee,
                        RefundAmount = refundAmount
                    };
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

             var reservation = await _context.Reservations
                .Include(r => r.Table)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            if (!string.Equals(reservation.CustomerEmail, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

             if (reservation.IsCancelled)
            {
                TempData["StatusMessage"] = "Cette réservation a déjà été annulée.";
                return RedirectToAction(nameof(Index));
            }

            var tablePricePerPerson = reservation.Table != null
                ? Math.Round(reservation.Table.BasePricePerPerson * TableCategoryMultipliers[reservation.Table.Category], 2, MidpointRounding.AwayFromZero)
                : 0m;

            var occasionPricePerPerson = GetOccasionPrice(reservation.Occasion);
            var calculatedTotal = Math.Round(reservation.NumberOfPeople * (tablePricePerPerson + occasionPricePerPerson), 2, MidpointRounding.AwayFromZero);
            var totalPrice = reservation.TotalPrice > 0 ? reservation.TotalPrice : calculatedTotal;

            var cancellationFee = Math.Round(totalPrice * 0.05m, 2, MidpointRounding.AwayFromZero);
            var refundAmount = Math.Max(totalPrice - cancellationFee, 0);

            reservation.IsCancelled = true;
            reservation.CancelledAt = DateTime.UtcNow;
            reservation.CancellationFee = cancellationFee;
            reservation.RefundAmount = refundAmount;
            reservation.TotalPrice = totalPrice;
            await _context.SaveChangesAsync();

             var subject = "[RestoBooking] Confirmation d'annulation de votre réservation";
            var body = $@"
                <h2>Bonjour {reservation.CustomerName},</h2>
                <p>Votre réservation chez <strong>RestoBooking</strong> a bien été annulée.</p>
                <ul>
                    <li><strong>Date et heure initiales :</strong> {reservation.ReservationDate:dd/MM/yyyy HH:mm}</li>
                    <li><strong>Table :</strong> {reservation.Table?.Name ?? "Table"} ({(reservation.Table != null ? GetDisplayName(reservation.Table.Category) : "")})</li>
                    <li><strong>Montant total initial :</strong> {totalPrice:0.00} DH</li>
                    <li><strong>Frais d'annulation (5%) :</strong> {cancellationFee:0.00} DH</li>
                    <li><strong>Montant remboursé :</strong> {refundAmount:0.00} DH</li>
                </ul>
                <p>Le montant remboursé vous sera retourné selon le moyen de paiement utilisé.</p>
                <p>Merci de votre compréhension.</p>
            ";

            await _emailService.SendEmail(reservation.CustomerEmail, subject, body);

            TempData["StatusMessage"] = $"Votre réservation a été annulée. Frais : {cancellationFee:0.00} DH. Remboursement : {refundAmount:0.00} DH.";
            return RedirectToAction(nameof(Index));
        }

        private static decimal GetOccasionPrice(OccasionType occasion)
        {
            return OccasionPricesPerPerson.TryGetValue(occasion, out var price)
                ? price
                : 0m;
        }

        private static string GetDisplayName<TEnum>(TEnum value) where TEnum : struct, Enum
        {
            var member = typeof(TEnum).GetMember(value.ToString()).FirstOrDefault();
            var displayAttribute = member?.GetCustomAttribute<DisplayAttribute>();
            return displayAttribute?.Name ?? value.ToString();
        }

        private static string FormatDisplayName(IdentityUser user)
        {
            if (!string.IsNullOrWhiteSpace(user.UserName) && user.UserName!.Contains("@"))
            {
                return user.UserName.Split('@')[0];
            }

            if (!string.IsNullOrWhiteSpace(user.UserName))
            {
                return user.UserName!;
            }

            return user.Email ?? "Utilisateur";
        }
    }
}