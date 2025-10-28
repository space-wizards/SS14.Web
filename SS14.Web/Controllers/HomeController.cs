using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SS14.Web.Models;

namespace SS14.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return Redirect("/Identity/Account/Manage");
    }

    public IActionResult Privacy()
    {
        return Redirect("https://spacestation14.com/about/privacy/");
    }

    public IActionResult Contact()
    {
        return Redirect("https://spacestation14.com/about/contact/");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }

    public IActionResult MainWebsite()
    {
        return Redirect("https://spacestation14.com/");
    }
}
