using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ConfirmEmailChangeModel : PageModel
{
    private readonly SpaceUserManager _userManager;
    private readonly SignInManager<SpaceUser> _signInManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly AccountLogManager _accountLogManager;

    public ConfirmEmailChangeModel(
        SpaceUserManager userManager,
        SignInManager<SpaceUser> signInManager,
        ApplicationDbContext dbContext,
        AccountLogManager accountLogManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _accountLogManager = accountLogManager;
    }

    [TempData]
    public string StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string userId, string email, string code)
    {
        if (userId == null || email == null || code == null)
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
        var oldEmail = user.Email;
        var result = await _userManager.ChangeEmailAsync(user, email, code);
        if (!result.Succeeded)
        {
            StatusMessage = "Error changing email.";
            return Page();
        }

        await _accountLogManager.LogAndSave(
            user,
            new AccountLogEmailChanged(oldEmail, email),
            _accountLogManager.ActorWithIP(user));

        await tx.CommitAsync();

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Thank you for confirming your email change.";
        return Page();
    }
}