using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using RestoBooking.Models;

namespace RestoBooking.Controllers;

public class HomeController : Controller
{
    private readonly IWebHostEnvironment _environment;


    public HomeController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public IActionResult Index()
    {
        var galleryImages = GetGalleryImages();

        return View(new GalleryViewModel
        {
            ImageUrls = galleryImages
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Gallery()
    {
        var galleryImages = GetGalleryImages();

        return View(new GalleryViewModel
        {
            ImageUrls = galleryImages
        });
    }

    public IActionResult SpecialEvents()
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

    private IEnumerable<string> GetGalleryImages()
    {
        var galleryPath = Path.Combine(_environment.WebRootPath, "oriantal");
        Directory.CreateDirectory(galleryPath);

        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".webp" };
        return Directory
           .EnumerateFiles(galleryPath)
           .Where(file => allowedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
           .Select(Path.GetFileName)
           .Where(file => !string.IsNullOrWhiteSpace(file))
           .Select(file => Url.Content($"~/oriantal/{file}"))
           .ToList();
    }
    public IActionResult Contact()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
