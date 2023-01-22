using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SpaceUserManager _userManager;
    private readonly SignInManager<SpaceUser> _signInManager;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(SignInManager<SpaceUser> signInManager,
        ILogger<LoginModel> logger,
        SpaceUserManager userManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty] public InputModel Input { get; set; }

    public IList<AuthenticationScheme> ExternalLogins { get; set; }

    public string ReturnUrl { get; set; }

    [TempData] public string ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        [Display(Name = "Email or Username")]
        public string EmailOrUsername { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me?")] public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        returnUrl ??= Url.Content("~/");

        // Clear the existing external cookie to ensure a clean login process
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        ReturnUrl = returnUrl; 
    }

    public async Task<IActionResult> OnPostAsync(string returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByNameOrEmailAsync(Input.EmailOrUsername);

        if (user != null)
        {
            var emailUnconfirmed = _userManager.Options.SignIn.RequireConfirmedEmail &&
                                   !await _userManager.IsEmailConfirmedAsync(user);

            if (emailUnconfirmed)
            {
                ModelState.AddModelError(string.Empty,
                    "The email address for this account still needs to be confirmed. " +
                    "Please confirm your email address before trying to log in.");
                return Page();
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, set lockoutOnFailure: true
            var result = await _signInManager.PasswordSignInAsync(user, Input.Password, Input.RememberMe,
                lockoutOnFailure: false);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");
                return LocalRedirect(returnUrl);
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToPage("./LoginWith2fa", new {ReturnUrl = returnUrl, RememberMe = Input.RememberMe});
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                return RedirectToPage("./Lockout");
            }
        }

        ModelState.AddModelError(string.Empty, "Invalid login credentials.");
        return Page();

        // If we got this far, something failed, redisplay form
    }
}