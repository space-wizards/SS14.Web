using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ConfirmEmailModel : PageModel
{
    private readonly SpaceUserManager _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly AccountLogManager _accountLogManager;

    public ConfirmEmailModel(SpaceUserManager userManager, ApplicationDbContext dbContext, AccountLogManager accountLogManager)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _accountLogManager = accountLogManager;
    }

    [TempData]
    public string StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string userId, string code)
    {
        if (userId == null || code == null)
        {
            return RedirectToPage("/Index");
        }

        await using var tx = await _dbContext.Database.BeginTransactionAsync();
        
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{userId}'.");
        }

        code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));

        var result = await _userManager.ConfirmEmailAsync(user, code);

        if (result.Succeeded)
        {
            await _accountLogManager.LogAndSave(
                user,
                new AccountLogEmailConfirmedChanged(true),
                _accountLogManager.ActorWithIP(user));
        }
        
        await tx.CommitAsync();
        
        StatusMessage = result.Succeeded ? "Thank you for confirming your email. You can now use your account." : "Error confirming your email.";
        return Page();
    }
}