using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestoBooking.Data;
using RestoBooking.Models;
using RestoBooking.Models.ViewModels;
using RestoBooking.Services;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace RestoBooking.Controllers
{
    public class ReservationController : Controller
    {
        private readonly AppDbContext _context;
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

        public ReservationController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }
        private static void EnsureOccasionPricingCoverage()
        {
            foreach (var occasion in Enum.GetValues<OccasionType>())
            {
                if (!OccasionPricesPerPerson.ContainsKey(occasion))
                {
                    OccasionPricesPerPerson[occasion] = 0m;
                }
            }
        }

        private static decimal GetOccasionPrice(OccasionType occasion)
        {
            EnsureOccasionPricingCoverage();
            return OccasionPricesPerPerson.TryGetValue(occasion, out var price)
                ? price
                : 0m;
        }


        // ---------------------------------------------------------
        // CREATE (GET)  -> accessible à tout le monde (clients)
        // ---------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Create()
        {
           await LoadOptionsAsync();
            return View();
        }

        // ---------------------------------------------------------
        // CREATE (POST)
        // ---------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reservation reservation)
        {
            // 1️⃣ Heure obligatoire
            if (reservation.ReservationTime == null)
            {
                ModelState.AddModelError(nameof(reservation.ReservationTime),
                    "Veuillez choisir une heure.");
            }

            // Nettoyage basique des champs texte pour éviter les espaces accidentels
            reservation.CustomerName = reservation.CustomerName?.Trim() ?? string.Empty;
            reservation.CustomerEmail = reservation.CustomerEmail?.Trim() ?? string.Empty;
            reservation.CustomerPhone = reservation.CustomerPhone?.Trim() ?? string.Empty;

            // 2️⃣ Si une heure est fournie, on combine Date + Heure
            if (reservation.ReservationTime != null)
            {
                // Combine la date (jj/mm/aaaa) + l'heure (hh:mm)
                var combinedDateTime = reservation.ReservationDate.Date
                                     + reservation.ReservationTime.Value;

                // a) Date/heure dans le futur
                if (combinedDateTime < DateTime.Now)
                {
                    ModelState.AddModelError(nameof(reservation.ReservationDate),
                        "La date doit être dans le futur.");
                }

                // b) Heure entre 13:00 et 23:00
                var h = reservation.ReservationTime.Value;
                var min = new TimeSpan(13, 0, 0);
                var max = new TimeSpan(23, 0, 0);

                if (h < min || h > max)
                {
                    ModelState.AddModelError(nameof(reservation.ReservationTime),
                        "L'horaire doit être entre 13:00 et 23:00.");
                }
                else
                {
                    // ✅ OK : on remplace ReservationDate par date+heure complète
                    reservation.ReservationDate = combinedDateTime;
                }
            }
                var table = await _context.Tables.FirstOrDefaultAsync(t => t.Id == reservation.TableId && t.IsActive);
            if (table == null)
            {
                ModelState.AddModelError(nameof(reservation.TableId), "La table sélectionnée n'est pas disponible.");
            }        

             EnsureOccasionPricingCoverage();
            if (!OccasionPricesPerPerson.ContainsKey(reservation.Occasion))
            {
               ModelState.AddModelError(nameof(reservation.Occasion), "Occasion invalide.");
            }

            if (!ModelState.IsValid)
            {
                await LoadOptionsAsync();

                return View(reservation);
            }
            table!.BasePricePerPerson = table.BasePricePerPerson <= 0 ? 220m : table.BasePricePerPerson;
            var tablePricePerPerson = table.BasePricePerPerson * TableCategoryMultipliers[table.Category];
            var occasionPricePerPerson = GetOccasionPrice(reservation.Occasion);
            var total = reservation.NumberOfPeople * (tablePricePerPerson + occasionPricePerPerson);

            reservation.TotalPrice = Math.Round(total, 2, MidpointRounding.AwayFromZero);
            reservation.Table = table;

            // 3️⃣ Vérifier si la table est déjà réservée à ce créneau
            bool tableDejaReservee = await _context.Reservations.AnyAsync(r =>
                r.TableId == reservation.TableId &&
                r.ReservationDate == reservation.ReservationDate);

            if (tableDejaReservee)
            {
                ModelState.AddModelError(string.Empty,
                    "Cette table est déjà réservée à cette date et heure.");

                  await LoadOptionsAsync();

                return View(reservation);
            }

            // 4️⃣ Sauvegarde + email
            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();
            var subject = "[RestoBooking] Confirmation de votre réservation";
            var body = $@"
                <h2>Bonjour {reservation.CustomerName},</h2>
                <p>Merci pour votre réservation chez <strong>RestoBooking</strong>. Voici votre facture détaillée :</p>
                <ul>
                    <li><strong>Date et heure :</strong> {reservation.ReservationDate:dd/MM/yyyy HH:mm}</li>
                    <li><strong>Nombre de personnes :</strong> {reservation.NumberOfPeople}</li>
                    <li><strong>Table :</strong> {table.Name} ({GetDisplayName(table.Category)}) – {tablePricePerPerson:0.00} DH / personne</li>
                    <li><strong>Occasion :</strong> {GetDisplayName(reservation.Occasion)} – {occasionPricePerPerson:0.00} DH / personne</li>
                    <li><strong>Total estimé :</strong> {reservation.TotalPrice:0.00} DH</li>
                </ul>
                <p>Notes : {reservation.Notes}</p>
                <p>Nous restons disponibles pour toute modification ou demande spécifique.</p>
                <p>Cordialement,<br/>L'équipe RestoBooking</p>
            ";

            await _emailService.SendEmail(reservation.CustomerEmail, subject, body);

            return RedirectToAction("Success", new { id = reservation.Id });        }

        // ---------------------------------------------------------
        // PAGE DE SUCCÈS
        // ---------------------------------------------------------
        public async Task<IActionResult> Success(int id)
        {
             var reservation = await _context.Reservations
                .Include(r => r.Table)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return RedirectToAction("Create");
            }

            ViewBag.TablePrice = reservation.Table != null
                ? reservation.Table.BasePricePerPerson * TableCategoryMultipliers[reservation.Table.Category]
                : 0m;
            ViewBag.OccasionPrice = GetOccasionPrice(reservation.Occasion);
            ViewBag.TableCategoryLabel = reservation.Table != null ? GetDisplayName(reservation.Table.Category) : string.Empty;
            ViewBag.OccasionLabel = GetDisplayName(reservation.Occasion);

            return View(reservation);
        }

        // ---------------------------------------------------------
        // ADMIN + FILTRES  (RÉSERVÉ AUX ADMINS)
        // ---------------------------------------------------------
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Admin(DateTime? date, int? tableId)
        {
            var query = _context.Reservations
                .Include(r => r.Table)
                .AsQueryable();

            if (date.HasValue)
            {
                var d = date.Value.Date;
                query = query.Where(r => r.ReservationDate.Date == d);
            }

            if (tableId.HasValue && tableId.Value > 0)
            {
                query = query.Where(r => r.TableId == tableId.Value);
            }

            var reservations = await query
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();

                await LoadOptionsAsync();

            return View(reservations);
        }

        // ---------------------------------------------------------
        // DETAILS (ADMIN UNIQUEMENT)
        // ---------------------------------------------------------
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Table)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
                return NotFound();

            return View(reservation);
        }

        // ---------------------------------------------------------
        // EDIT (GET)  (ADMIN UNIQUEMENT)
        // ---------------------------------------------------------
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);

            if (reservation == null)
                return NotFound();

             await LoadOptionsAsync();
            return View(reservation);
        }

        // ---------------------------------------------------------
        // EDIT (POST) (ADMIN UNIQUEMENT)
        // ---------------------------------------------------------
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Reservation reservation)
        {
            if (id != reservation.Id)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadOptionsAsync();

                return View(reservation);
            }

            _context.Update(reservation);
            await _context.SaveChangesAsync();

            return RedirectToAction("Admin");
        }

        // ---------------------------------------------------------
        // DELETE (GET) (ADMIN UNIQUEMENT)
        // ---------------------------------------------------------
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Table)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
                return NotFound();

            return View(reservation);
        }

        // ---------------------------------------------------------
        // DELETE (POST) (ADMIN UNIQUEMENT)
        // ---------------------------------------------------------
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);

            if (reservation == null)
                return NotFound();

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();

            return RedirectToAction("Admin");
        }
        private async Task LoadOptionsAsync()
        {
            var tables = await _context.Tables
                .Where(t => t.IsActive)
                .ToListAsync();

                if (!tables.Any())
            {
                tables = new List<Table>
                {
                    new Table { Name = "Table Standard", Capacity = 4, Category = TableCategory.Standard, BasePricePerPerson = 240m, IsActive = true },
                    new Table { Name = "Table VIP", Capacity = 4, Category = TableCategory.VIPExclusive, BasePricePerPerson = 360m, IsActive = true }
                };

                await _context.Tables.AddRangeAsync(tables);
                await _context.SaveChangesAsync();
            }

            // Correction des tables existantes avec tarifs manquants pour assurer l'affichage dans le formulaire
            bool needsSave = false;
            foreach (var table in tables)
            {
                if (table.BasePricePerPerson <= 0)
                {
                    table.BasePricePerPerson = 220m;
                    needsSave = true;
                }
            }

            if (needsSave)
            {
                await _context.SaveChangesAsync();
            }

            ViewBag.TableOptions = tables.Select(t => new TableOptionViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Category = t.Category,
                CategoryLabel = GetDisplayName(t.Category),
                BasePricePerPerson = t.BasePricePerPerson,
                PricePerPerson = Math.Round(t.BasePricePerPerson * TableCategoryMultipliers[t.Category], 2, MidpointRounding.AwayFromZero)
            }).ToList();

            ViewBag.Occasions = Enum.GetValues<OccasionType>()
                .Select(o => new OccasionOptionViewModel
                {
                    Value = o,
                    Label = GetDisplayName(o),
                    PricePerPerson = GetOccasionPrice(o)
                })
                .ToList();
        }

        private static string GetDisplayName<TEnum>(TEnum value) where TEnum : struct, Enum
        {
            var member = typeof(TEnum).GetMember(value.ToString()).FirstOrDefault();
            var displayAttribute = member?.GetCustomAttribute<DisplayAttribute>();
            return displayAttribute?.Name ?? value.ToString();
        }
    }
}
