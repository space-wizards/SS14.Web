using System;
using System.Collections.Generic;
using System.Linq;
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

namespace SS14.Web.Areas.Identity.Pages.Account.Manage;

public class ManagePatreon : PageModel
{
    private readonly SpaceUserManager _userManager;
    private readonly ILogger<ManagePatreon> _logger;
    private readonly ApplicationDbContext _db;
    private readonly IOptions<PatreonConfiguration> _cfg;
    private readonly AccountLogManager _accountLogManager;

    public bool PatreonLinked { get; private set; }
    public string PatreonTier { get; private set; }

    [BindProperty] public List<SharingInput> Sharing { get; set; } = new();

    [TempData] public string StatusMessage { get; set; }

    public sealed class SharingInput
    {
        public int CampaignId { get; set; }
        public string CampaignName { get; set; }
        public bool Share { get; set; }
        public string Tier { get; set; }
    }

    public ManagePatreon(
        SpaceUserManager userManager,
        ILogger<ManagePatreon> logger,
        ApplicationDbContext db,
        IOptions<PatreonConfiguration> cfg,
        AccountLogManager accountLogManager)
    {
        _userManager = userManager;
        _logger = logger;
        _db = db;
        _cfg = cfg;
        _accountLogManager = accountLogManager;
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

        if (patron != null)
        {
            var membership = await _db.PatreonMemberships
                .Where(m => m.Campaign.IsFirstParty && m.Campaign.Enabled && m.PatreonUserId == patron.PatreonId)
                .Select(m => new { m.CurrentTier, m.Campaign.TierNames })
                .SingleOrDefaultAsync();

            if (membership?.CurrentTier is { } t)
            {
                PatreonTier = membership.TierNames.GetValueOrDefault(t, t);
            }

            var communities = await _db.PatreonMemberships
                .Where(m => !m.Campaign.IsFirstParty && m.PatreonUserId == patron.PatreonId)
                .OrderBy(m => m.Campaign.Name)
                .Select(m => new
                {
                    m.CampaignId,
                    CampaignName = m.Campaign.Name,
                    m.Hidden,
                    m.CurrentTier,
                    m.Campaign.TierNames
                })
                .ToListAsync();

            Sharing = communities
                .Select(m => new SharingInput
                {
                    CampaignId = m.CampaignId,
                    CampaignName = m.CampaignName,
                    Share = !m.Hidden,
                    Tier = m.CurrentTier == null ? null : m.TierNames.GetValueOrDefault(m.CurrentTier, m.CurrentTier)
                })
                .ToList();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSharingAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        var patron = await _db.Patrons.SingleOrDefaultAsync(p => p.SpaceUserId == user.Id);
        if (patron == null)
        {
            return RedirectToPage();
        }

        // Only touch the user's own memberships, the campaign IDs come from the form
        var memberships = await _db.PatreonMemberships
            .Where(m => !m.Campaign.IsFirstParty && m.PatreonUserId == patron.PatreonId)
            .ToListAsync();

        foreach (var membership in memberships)
        {
            var input = Sharing.FirstOrDefault(s => s.CampaignId == membership.CampaignId);
            if (input != null)
            {
                membership.Hidden = !input.Share;
            }
        }

        await _db.SaveChangesAsync();

        StatusMessage = "Sharing preferences saved.";
        return RedirectToPage();
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
            await _accountLogManager.LogAndSave(user, new AccountLogPatreonUnlinked());
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
