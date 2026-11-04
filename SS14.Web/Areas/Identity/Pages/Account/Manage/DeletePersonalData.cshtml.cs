using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SS14.Auth.Shared;
using SS14.Auth.Shared.Config;
using SS14.Auth.Shared.Data;
using SS14.Auth.Shared.Emails;

namespace SS14.Web.Areas.Identity.Pages.Account.Manage
{
    public class DeletePersonalDataModel : PageModel
    {
        private readonly SpaceUserManager _userManager;
        private readonly ILogger<DeletePersonalDataModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IOptions<AccountConfiguration> _configuration;

        public DeletePersonalDataModel(
            SpaceUserManager userManager,
            ILogger<DeletePersonalDataModel> logger,
            IEmailSender emailSender,
            IOptions<AccountConfiguration> configuration)
        {
            _userManager = userManager;
            _logger = logger;
            _emailSender = emailSender;
            _configuration = configuration;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public bool RequirePassword { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            RequirePassword = await _userManager.HasPasswordAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostCancelAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await _userManager.CancelQueuedDeletion(user);

            await ModelShared.SendDeletionCanceledEmail(_emailSender, user.Email);
            _logger.LogInformation("User with ID '{UserId}' canceled their deletion.", user.Id);

            StatusMessage = "Account deletion canceled.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            RequirePassword = await _userManager.HasPasswordAsync(user);
            if (RequirePassword)
            {
                if (!await _userManager.CheckPasswordAsync(user, Input.Password))
                {
                    ModelState.AddModelError(string.Empty, "Incorrect password.");
                    return Page();
                }
            }

            var userId = await _userManager.GetUserIdAsync(user);
            await _userManager.QueueDeletion(user);

            var gracePeriod = _configuration.Value.UserDeletionGracePeriodDays;
            var deletionDate = DateTime.UtcNow.AddDays(gracePeriod);

            await ModelShared.SendDeletionQueuedEmail(_emailSender, user.Email, deletionDate);
            _logger.LogInformation("User with ID '{UserId}' queued their deletion.", userId);

            StatusMessage = "Account deletion queued.";
            return RedirectToPage();
        }
    }
}

