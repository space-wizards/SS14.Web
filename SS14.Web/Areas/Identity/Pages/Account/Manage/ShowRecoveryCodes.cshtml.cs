using System;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Serilog;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Areas.Identity.Pages.Account.Manage;

public class ShowRecoveryCodesModel : PageModel
{
    private readonly SpaceUserManager _userManager;
    private readonly ApplicationDbContext _dbContext;

    public ShowRecoveryCodesModel(
        SpaceUserManager userManager,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    [TempData]
    public string[] RecoveryCodes { get; set; }

    [TempData]
    public string StatusMessage { get; set; }

    public IActionResult OnGet()
    {
        if (RecoveryCodes == null || RecoveryCodes.Length == 0)
        {
            return RedirectToPage("./TwoFactorAuthentication");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDownloadRecoveryCodes()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        var rawValue = _dbContext.UserTokens
            .Where(x => x.UserId == user.Id && x.Name == "RecoveryCodes")
            .Select(q => q.Value)
            .FirstOrDefault();

        var recoveryCodes = rawValue?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            ?? [];

        var header = $"These are the 2fa recovery codes for the Space Station 14 account {user.UserName}. Keep them in a safe place.\n\n";
        var text = header + string.Join("\n", recoveryCodes);

        Response.Headers.Add("Content-Disposition", $"attachment; filename=SS14-{user.UserName}-Recovery.txt");
        return new FileContentResult(Encoding.UTF8.GetBytes(text), MediaTypeNames.Text.Plain);
    }
}
