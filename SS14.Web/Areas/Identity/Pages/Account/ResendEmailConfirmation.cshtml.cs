using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using SS14.Auth.Shared;
using SS14.Auth.Shared.Data;
using SS14.Auth.Shared.Emails;
using SS14.Web.HCaptcha;

namespace SS14.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ResendEmailConfirmationModel : PageModel
{
    private readonly UserManager<SpaceUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly HCaptchaService _hCaptcha;

    public ResendEmailConfirmationModel(
        UserManager<SpaceUser> userManager,
        IEmailSender emailSender,
        HCaptchaService hCaptcha)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _hCaptcha = hCaptcha;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    [BindProperty(Name = "h-captcha-response")]
    public string HCaptchaResponse { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        
        if (!await _hCaptcha.ValidateHCaptcha(HCaptchaResponse, ModelState))
            return Page();

        var email = Input.Email.Trim();

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Verification email sent. Please check your email.");
            return Page();
        }

        var userId = await _userManager.GetUserIdAsync(user);
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var confirmLink = Url.Page(
            "/Account/ConfirmEmail",
            pageHandler: null,
            values: new { userId = userId, code = code },
            protocol: Request.Scheme);

        await ModelShared.SendConfirmEmail(_emailSender, email, confirmLink);

        ModelState.AddModelError(string.Empty, "Verification email sent. Please check your email.");
        return Page();
    }
}