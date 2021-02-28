using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SS14.Auth.Shared.Data;
using SS14.Web.Config;

namespace SS14.Web.Areas.Identity.Pages.Account.Manage
{
    public class ManagePatreon : PageModel
    {
        private readonly UserManager<SpaceUser> _userManager;
        private readonly ILogger<ManagePatreon> _logger;
        private readonly ApplicationDbContext _db;
        private readonly IOptions<PatreonConfiguration> _cfg;

        public bool PatreonLinked { get; private set; }
        public string PatreonTier { get; private set; }

        public ManagePatreon(
            UserManager<SpaceUser> userManager,
            ILogger<ManagePatreon> logger,
            ApplicationDbContext db,
            IOptions<PatreonConfiguration> cfg)
        {
            _userManager = userManager;
            _logger = logger;
            _db = db;
            _cfg = cfg;
        }

        public async Task<IActionResult> OnGet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var patron = await _db.Patrons.SingleOrDefaultAsync(p => p.SpaceUserId == user.Id);

            PatreonLinked = patron != null;

            if (patron?.CurrentTier is { } t)
            {
                if (_cfg.Value.TierNames.TryGetValue(t, out var name))
                {
                    PatreonTier = name;
                }
                else
                {
                    PatreonTier = t;
                }
            }
            return Page();
        }

        public async Task<IActionResult> OnPostUnlinkPatreonAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var patron = await _db.Patrons.SingleOrDefaultAsync(p => p.SpaceUserId == user.Id);

            if (patron != null)
            {
                _db.Patrons.Remove(patron);
                await _db.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostLinkPatreonAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var redirect = Url.Page("./ManagePatreon");

            return Challenge(new AuthenticationProperties
            {
                Items =
                {
                    ["SS14UserId"] = user.Id.ToString(),
                },
                RedirectUri = redirect
            }, "Patreon");
        }
    }
}