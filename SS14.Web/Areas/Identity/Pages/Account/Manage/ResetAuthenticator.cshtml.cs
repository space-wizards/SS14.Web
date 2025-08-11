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

public class ResetAuthenticatorModel : PageModel
{
    private readonly SpaceUserManager _userManager;
    private readonly SignInManager<SpaceUser> _signInManager;
    private readonly ApplicationDbContext _dbContext;
    ILogger<ResetAuthenticatorModel> _logger;
    private readonly AccountLogManager _accountLogManager;

    public ResetAuthenticatorModel(
        SpaceUserManager userManager,
        SignInManager<SpaceUser> signInManager,
        ApplicationDbContext dbContext,
        ILogger<ResetAuthenticatorModel> logger,
        AccountLogManager accountLogManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _logger = logger;
        _accountLogManager = accountLogManager;
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

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        _logger.LogInformation("User with ID '{UserId}' has reset their authentication app key.", user.Id);

        await _accountLogManager.LogAndSave(user, new AccountLogAuthenticatorReset());

        await tx.CommitAsync();
        
        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Your authenticator app key has been reset, you will need to configure your authenticator app using the new key.";

        return RedirectToPage("./EnableAuthenticator");
    }
}