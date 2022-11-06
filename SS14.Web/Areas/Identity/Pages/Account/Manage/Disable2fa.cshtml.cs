using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Areas.Identity.Pages.Account.Manage;

public class Disable2faModel : PageModel
{
    private readonly SpaceUserManager _userManager;
    private readonly SignInManager<SpaceUser> _signInManager;
    private readonly ILogger<Disable2faModel> _logger;
    private readonly ApplicationDbContext _dbContext;

    public Disable2faModel(
        SpaceUserManager userManager,
        SignInManager<SpaceUser> signInManager,
        ILogger<Disable2faModel> logger,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _dbContext = dbContext;
    }

    [TempData]
    public string StatusMessage { get; set; }

    public async Task<IActionResult> OnGet()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        if (!await _userManager.GetTwoFactorEnabledAsync(user))
        {
            throw new InvalidOperationException($"Cannot disable 2FA for user with ID '{_userManager.GetUserId(User)}' as it's not currently enabled.");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        await using var tx = await _dbContext.Database.BeginTransactionAsync();
        
        _userManager.AccountLog(
            user,
            AccountLogType.AuthenticatorDisabled,
            new AccountLogAuthenticatorDisabled(user.Id));

        var disable2faResult = await _userManager.SetTwoFactorEnabledAsync(user, false);
        if (!disable2faResult.Succeeded)
        {
            throw new InvalidOperationException($"Unexpected error occurred disabling 2FA for user with ID '{_userManager.GetUserId(User)}'.");
        }

        await tx.CommitAsync();
        
        await _signInManager.RefreshSignInAsync(user);
        
        _logger.LogInformation("User with ID '{UserId}' has disabled 2FA.", _userManager.GetUserId(User));
        StatusMessage = "2FA has been disabled. You can re-enable 2FA when you setup an authenticator app";
        return RedirectToPage("./TwoFactorAuthentication");
    }
}