#nullable enable
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.Auth.Shared.Data;
using SS14.Web.OpenId.Services;

namespace SS14.Web.Areas.Identity.Pages.Account.Manage.OAuthApps;

public class ConfirmDelete : PageModel
{
    private readonly UserManager<SpaceUser> _userManager;
    private readonly SpaceApplicationManager _appManager;

    public SpaceApplication App { get; set; } = null!;

    public ConfirmDelete(UserManager<SpaceUser> userManager, SpaceApplicationManager appManager)
    {
        _userManager = userManager;
        _appManager = appManager;
    }

    public async Task<IActionResult> OnGetAsync(string client)
    {
        var user = await _userManager.GetUserAsync(User);
        var app = await _appManager.FindByIdAsync(client);
        if (app == null)
            return NotFound();

        if (app.SpaceUserId != user!.Id)
            return Forbid();

        App = app;
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string client)
    {
        var user = await _userManager.GetUserAsync(User);
        var app = await _appManager.FindByIdAsync(client);
        if (app == null)
            return NotFound();

        if (app.SpaceUserId != user!.Id)
            return Forbid();

        await _appManager.DeleteAsync(app);
        return RedirectToPage("../Developer");
    }
}
