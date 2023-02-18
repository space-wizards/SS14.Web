using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SS14.Web.Models;

namespace SS14.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return Redirect("https://spacestation14.io/about/contact/");
    }

    public IActionResult MoveSession(string accessToken = null, string returnUrl = "/")
    {
        if (accessToken == null)
        {
            return BadRequest("A accessToken is required.");
        }
        
        Response.Cookies.Append(".AspNetCore.Identity.Application", accessToken, new CookieOptions
        {
            Secure = true,
            HttpOnly = true,
            SameSite = SameSiteMode.None,
        });

        return LocalRedirect(returnUrl ?? "/");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }

    public IActionResult MainWebsite()
    {
        return Redirect("https://spacestation14.io/");
    }
}
