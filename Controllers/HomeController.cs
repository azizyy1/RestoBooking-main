using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RestoBooking.Models;

namespace RestoBooking.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
