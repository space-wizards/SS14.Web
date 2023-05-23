using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Areas.Identity.Pages.Account.Manage
{
    public class ManageDiscord : PageModel
    {
        private readonly UserManager<SpaceUser> _userManager;
        private readonly ILogger<ManageDiscord> _logger;
        private readonly ApplicationDbContext _db;

        public bool DiscordLinked { get; private set; }

        public ManageDiscord(
            UserManager<SpaceUser> userManager,
            ILogger<ManageDiscord> logger,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> OnGet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            DiscordLinked = user.DiscordId != null;

            return Page();
        }

        public async Task<IActionResult> OnPostUnlinkDiscordAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            user.DiscordId = null;
            await _db.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostLinkDiscordAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var redirect = Url.Page("./ManageDiscord");

            return Challenge(new AuthenticationProperties
            {
                Items =
                {
                    ["SS14UserId"] = user.Id.ToString(),
                },
                RedirectUri = redirect
            }, "Discord");
        }
    }
}
