using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SS14.Auth.Shared.Data;
using SS14.Auth.Shared.Emails;

namespace SS14.Web.Areas.Identity.Pages.Account.Manage;

public class ResetAuthenticatorModel : PageModel
{
    private readonly SpaceUserManager _userManager;
    private readonly SignInManager<SpaceUser> _signInManager;
    private readonly IEmailSender _emailSender;
    private readonly ApplicationDbContext _dbContext;
    ILogger<ResetAuthenticatorModel> _logger;
    private readonly AccountLogManager _accountLogManager;

    public ResetAuthenticatorModel(
        SpaceUserManager userManager,
        SignInManager<SpaceUser> signInManager,
        IEmailSender emailSender,
        ApplicationDbContext dbContext,
        ILogger<ResetAuthenticatorModel> logger,
        AccountLogManager accountLogManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
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

        var userEmail = await _userManager.GetEmailAsync(user);
        await _emailSender.SendEmailAsync(userEmail,
            "Your Space Station 14 account 2fa was reset",
            $"This email was sent to you to confirm that 2fa has been reset on your account. If this was you feel free to ignore this email." +
            $"\n\nIf this was not you, send an email to support@spacestation14.com immediately.");

        return RedirectToPage("./EnableAuthenticator");
    }
}
