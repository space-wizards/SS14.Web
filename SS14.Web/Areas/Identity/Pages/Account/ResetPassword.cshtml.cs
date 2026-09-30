using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ResetPasswordModel : PageModel
{
    private readonly SpaceUserManager _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly AccountLogManager _logManager;
    private readonly ILogger<ResetPasswordModel> _logger;

    public ResetPasswordModel(SpaceUserManager userManager, ApplicationDbContext dbContext, AccountLogManager logManager, ILogger<ResetPasswordModel> logger)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _logManager = logManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }

    public IActionResult OnGet(string code = null)
    {
        if (code == null)
        {
            return BadRequest("A code must be supplied for password reset.");
        }
        else
        {
            Input = new InputModel
            {
                Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code))
            };
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await using var tx = await _dbContext.Database.BeginTransactionAsync();

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user == null)
        {
            // Don't reveal that the user does not exist
            _logger.LogInformation("Password reset attempt for user that doesn't exist");
            return RedirectToPage("./ResetPasswordConfirmation");
        }

        var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);

        if (result.Succeeded)
        {
            await _logManager.LogAndSave(user, new AccountLogPasswordChanged(), _logManager.ActorWithIP(user));
        }

        await tx.CommitAsync();

        if (result.Succeeded)
        {
            _logger.LogInformation("Password reset for account: {AccountId}", user.Id);
            return RedirectToPage("./ResetPasswordConfirmation");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return Page();
    }
}
