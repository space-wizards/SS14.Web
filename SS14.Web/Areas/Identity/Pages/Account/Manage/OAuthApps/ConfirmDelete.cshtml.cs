using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Areas.Identity.Pages.Account.Manage.OAuthApps;

// TODO: Replace identityserver4 code in this file

public class ConfirmDelete : PageModel
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<SpaceUser> _userManager;

    public ConfirmDelete(ApplicationDbContext dbContext, UserManager<SpaceUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    //public UserOAuthClient App { get; set; }

    public async Task<IActionResult> OnGetAsync(int client)
    {
        var user = await _userManager.GetUserAsync(User);
        //App = await _dbContext.UserOAuthClients.Include(a => a.Client)
        //    .SingleOrDefaultAsync(ac => ac.UserOAuthClientId == client);

        //if (App == null)
        //    return NotFound();

        //if (!Manage.VerifyAppAccess(user, App))
        //    return Forbid();

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int client)
    {
        var user = await _userManager.GetUserAsync(User);
        //App = await _dbContext.UserOAuthClients.Include(a => a.Client)
        //    .SingleOrDefaultAsync(ac => ac.UserOAuthClientId == client);

        //if (App == null)
        //    return NotFound();

        //if (!Manage.VerifyAppAccess(user, App))
        //    return Forbid();

       // _dbContext.Remove(App.Client);

        await _dbContext.SaveChangesAsync();

        return RedirectToPage("../Developer");
    }
}
