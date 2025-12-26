using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RestoBooking.Models;

namespace RestoBooking.Controllers;

public class HomeController : Controller
{
    private readonly IWebHostEnvironment _environment;

    private static readonly string[] DefaultGalleryImages =
    [
        "https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=1200&q=80",
        "https://images.unsplash.com/photo-1421622548261-c45bfe178854?auto=format&fit=crop&w=1200&q=80",
        "https://images.unsplash.com/photo-1504674926905-4f27e6f1c1e8?auto=format&fit=crop&w=1200&q=80",
        "https://images.unsplash.com/photo-1466978913421-dad2ebd01d17?auto=format&fit=crop&w=1200&q=80",
        "https://images.unsplash.com/photo-1467003909585-2f8a72700288?auto=format&fit=crop&w=1200&q=80",
        "https://images.unsplash.com/photo-1504274066651-8d31a536b11a?auto=format&fit=crop&w=1200&q=80",
        "https://images.unsplash.com/photo-1470337458703-46ad1756a187?auto=format&fit=crop&w=1200&q=80",
        "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?auto=format&fit=crop&w=1200&q=80"
    ];

    public HomeController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public IActionResult Index()
    {
        var galleryImages = GetGalleryImages(out var hasUploads);

        return View(new GalleryViewModel
        {
            ImageUrls = galleryImages,
            HasUploadedImages = hasUploads,
            StatusMessage = TempData["UploadStatus"] as string
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Portfolio()
    {
        return View();
    }

    public IActionResult CuisineMarocaine()
    {
        return View();
    }

    public IActionResult CuisineItalienne()
    {
        return View();
    }

    public IActionResult Steakhouse()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadGalleryImage(IFormFile? imageFile)
    {
        if (imageFile is null || imageFile.Length == 0)
        {
            TempData["UploadStatus"] = "Aucun fichier n'a été sélectionné.";
            return RedirectToAction(nameof(Index));
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            TempData["UploadStatus"] = "Format d'image non pris en charge (jpg, jpeg, png, webp).";
            return RedirectToAction(nameof(Index));
        }

        const long maxSize = 5 * 1024 * 1024;
        if (imageFile.Length > maxSize)
        {
            TempData["UploadStatus"] = "Le fichier est trop volumineux (5 Mo maximum).";
            return RedirectToAction(nameof(Index));
        }

        var galleryPath = Path.Combine(_environment.WebRootPath, "images", "gallery");
        Directory.CreateDirectory(galleryPath);

        var safeFileName = $"gallery-{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(galleryPath, safeFileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await imageFile.CopyToAsync(stream);

        TempData["UploadStatus"] = "Votre photo a été ajoutée à la galerie.";
        return RedirectToAction(nameof(Index));
    }

    private IEnumerable<string> GetGalleryImages(out bool hasUploads)
    {
        var galleryPath = Path.Combine(_environment.WebRootPath, "images", "gallery");
        Directory.CreateDirectory(galleryPath);

        var localImages = Directory
            .EnumerateFiles(galleryPath)
            .Select(Path.GetFileName)
            .Where(file => !string.IsNullOrWhiteSpace(file))
            .Select(file => Url.Content($"~/images/gallery/{file}"))
            .ToList();

        hasUploads = localImages.Count > 0;

        return hasUploads ? localImages : DefaultGalleryImages;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
