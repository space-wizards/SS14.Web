using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SS14.Auth.Shared.Config;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Areas.Identity.Pages.Account.Manage
{
    public class ManageDiscord : PageModel
    {
        private readonly UserManager<SpaceUser> _userManager;
        private readonly ILogger<ManageDiscord> _logger;
        private readonly ApplicationDbContext _db;
        private readonly IOptions<DiscordConfiguration> _cfg;

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

            var discord = await _db.Discords.SingleOrDefaultAsync(p => p.SpaceUserId == user.Id);

            DiscordLinked = discord != null;

            return Page();
        }

        public async Task<IActionResult> OnPostUnlinkDiscordAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var discord = await _db.Discords.SingleOrDefaultAsync(p => p.SpaceUserId == user.Id);

            if (discord != null)
            {
                _db.Discords.Remove(discord);
                await _db.SaveChangesAsync();
            }

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