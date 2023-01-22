using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Areas.Admin.Pages.Users;

public class ConfirmClearTfa : PageModel
{
    private readonly SpaceUserManager _userManager;
    private readonly ApplicationDbContext _dbContext;

    public ConfirmClearTfa(SpaceUserManager userManager, ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public SpaceUser SpaceUser { get; set; }
        
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        SpaceUser = await _userManager.FindByIdAsync(id.ToString());

        if (SpaceUser == null)
        {
            return NotFound("Unknown user");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostClearAsync(Guid id)
    {
        var actor = await _userManager.GetUserAsync(User);
        SpaceUser = await _userManager.FindByIdAsync(id.ToString());

        if (SpaceUser == null)
        {
            return NotFound("Unknown user");
        }

        await using var tx = await _dbContext.Database.BeginTransactionAsync();
        
        _userManager.AccountLog(
            SpaceUser, 
            AccountLogType.AuthenticatorReset,
            new AccountLogAuthenticatorReset(actor.Id));
        
        await _userManager.SetTwoFactorEnabledAsync(SpaceUser, false);
        await _userManager.ResetAuthenticatorKeyAsync(SpaceUser);

        await tx.CommitAsync();
        
        TempData["StatusMessage"] = "2FA cleared & disabled";
        return RedirectToPage("./Index");
    }
}