using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using SS14.Auth.Shared;
using SS14.Auth.Shared.Data;
using SS14.Auth.Shared.Emails;
using SS14.Auth.Shared.Sessions;

namespace SS14.Web.Areas.Admin.Pages.Users
{
    public class ViewUser : PageModel
    {
        private readonly UserManager<SpaceUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly SessionManager _sessionManager;
        private readonly PatreonDataManager _patreonDataManager;

        public SpaceUser SpaceUser { get; set; }

        [TempData] public string StatusMessage { get; set; }

        [BindProperty] public InputModel Input { get; set; }

        public string PatronTier { get; set; }
        
        public class InputModel
        {
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Display(Name = "Username")] public string Username { get; set; }

            [Display(Name = "Email Confirmed?")] public bool EmailConfirmed { get; set; }

            [Display(Name = "Is Hub Admin?")] public bool HubAdmin { get; set; }
        }

        public ViewUser(UserManager<SpaceUser> userManager, IEmailSender emailSender, SessionManager sessionManager, PatreonDataManager patreonDataManager)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _sessionManager = sessionManager;
            _patreonDataManager = patreonDataManager;
        }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            SpaceUser = await _userManager.FindByIdAsync(id.ToString());

            if (SpaceUser == null)
            {
                return NotFound("That user does not exist!");
            }

            await LoadAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync(Guid id)
        {
            SpaceUser = await _userManager.FindByIdAsync(id.ToString());

            if (SpaceUser == null)
            {
                return NotFound("That user does not exist!");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync();
                return Page();
            }

            SpaceUser.Email = Input.Email;
            SpaceUser.UserName = Input.Username;
            SpaceUser.EmailConfirmed = Input.EmailConfirmed;

            if (Input.HubAdmin != await _userManager.IsInRoleAsync(SpaceUser, AuthConstants.RoleSysAdmin))
            {
                if (Input.HubAdmin)
                {
                    await _userManager.AddToRoleAsync(SpaceUser, AuthConstants.RoleSysAdmin);
                }
                else
                {
                    await _userManager.RemoveFromRoleAsync(SpaceUser, AuthConstants.RoleSysAdmin);
                }

                await _userManager.UpdateSecurityStampAsync(SpaceUser);
            }

            await _userManager.UpdateAsync(SpaceUser);

            StatusMessage = "Changes saved";

            return RedirectToPage(new {id});
        }

        public async Task<IActionResult> OnPostResendConfirmationAsync(Guid id)
        {
            SpaceUser = await _userManager.FindByIdAsync(id.ToString());

            if (SpaceUser == null)
            {
                return NotFound("That user does not exist!");
            }
            
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(SpaceUser);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var confirmLink = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new {userId = id, code = code},
                protocol: Request.Scheme);

            try
            {
                await ModelShared.SendConfirmEmail(_emailSender, SpaceUser.Email, confirmLink);
                StatusMessage = "Email sent";
            }
            catch (Exception e)
            {
                // Looks awful but better than nothing.
                StatusMessage = $"Error while sending email: {e}";
            }

            return RedirectToPage(new {id});
        }

        public async Task<IActionResult> OnPostLogoutAsync(Guid id)
        {
            SpaceUser = await _userManager.FindByIdAsync(id.ToString());

            if (SpaceUser == null)
            {
                return NotFound("That user does not exist!");
            }

            await _sessionManager.InvalidateSessions(SpaceUser);
            await _userManager.UpdateSecurityStampAsync(SpaceUser);

            StatusMessage = "All sessions logged out";
            
            return RedirectToPage(new {id});
        }

        private async Task LoadAsync()
        {
            Input = new InputModel
            {
                Email = SpaceUser.Email,
                EmailConfirmed = SpaceUser.EmailConfirmed,
                Username = SpaceUser.UserName,
                HubAdmin = await _userManager.IsInRoleAsync(SpaceUser, AuthConstants.RoleSysAdmin)
            };

            PatronTier = await _patreonDataManager.GetPatreonTierAsync(SpaceUser);
        }
    }
}