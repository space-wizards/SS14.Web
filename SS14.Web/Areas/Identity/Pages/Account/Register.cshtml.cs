#nullable enable
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using SS14.Auth.Shared;
using SS14.Auth.Shared.Data;
using SS14.Auth.Shared.Emails;
using SS14.Web.HCaptcha;
using SS14.Web.Helpers;

namespace SS14.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly SignInManager<SpaceUser> _signInManager;
    private readonly UserManager<SpaceUser> _userManager;
    private readonly ILogger<RegisterModel> _logger;
    private readonly IEmailSender _emailSender;
    private readonly ISystemClock _systemClock;
    private readonly HCaptchaService _hCaptcha;

    public RegisterModel(
        UserManager<SpaceUser> userManager,
        SignInManager<SpaceUser> signInManager,
        ILogger<RegisterModel> logger,
        IEmailSender emailSender,
        ISystemClock systemClock,
        HCaptchaService hCaptcha)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _emailSender = emailSender;
        _systemClock = systemClock;
        _hCaptcha = hCaptcha;
    }

    [BindProperty] public InputModel Input { get; set; }

    [BindProperty(Name = "h-captcha-response")]
    public string HCaptchaResponse { get; set; }

    public string ReturnUrl { get; set; }

    public IList<AuthenticationScheme> ExternalLogins { get; set; }

    public class InputModel
    {
        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.",
            MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "I am 13 years of age or older")]
        [MustBeTrue(ErrorMessage = "You must be 13 or older to sign up for Space Station 14.")]
        public bool AgeCheck { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
    }

    public async Task<IActionResult> OnPostAsync(string returnUrl = null)
    {
        var userName = Input.Username.Trim();
        var email = Input.Email.Trim();

        returnUrl ??= Url.Content("~/");
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        if (!ModelState.IsValid)
            return Page();

        if (!await _hCaptcha.ValidateHCaptcha(HCaptchaResponse, ModelState))
            return Page();

        var user = ModelShared.CreateNewUser(userName, email, _systemClock);
        var result = await _userManager.CreateAsync(user, Input.Password);
        if (result.Succeeded)
        {
            _logger.LogInformation("User created a new account with password.");

            var confirmLink = await GenerateEmailConfirmLink(user, returnUrl);

            await ModelShared.SendConfirmEmail(_emailSender, email, confirmLink);

            if (_userManager.Options.SignIn.RequireConfirmedAccount)
            {
                return RedirectToPage("RegisterConfirmation", new { email = email, returnUrl = returnUrl });
            }
            else
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(returnUrl);
            }
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        // If we got this far, something failed, redisplay form
        return Page();
    }

    public async Task<string?> GenerateEmailConfirmLink(
        SpaceUser user,
        string? returnUrl = null)
    {
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var callbackUrl = Url.Page(
            "/Account/ConfirmEmail",
            pageHandler: null,
            values: new
            {
                area = "Identity",
                userId = user.Id,
                code = code,
                returnUrl = returnUrl,
            },
            protocol: Request.Scheme);
        return callbackUrl;
    }
}