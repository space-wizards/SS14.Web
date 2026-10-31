using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SS14.Auth.Shared.Data;
using SS14.Auth.Shared.Patreon;

namespace SS14.Web.Areas.Identity.Pages.Account.Manage;

public class ManagePatreonCampaigns : PageModel
{
    private const int MaxRewardLength = 64;

    private static readonly Dictionary<string, string> ResultMessages = new()
    {
        [PatreonCampaignConnectHandler.ResultConnected] = "Your campaign is connected and its members were imported.",
        [PatreonCampaignConnectHandler.ResultNoCampaign] = "That Patreon account has no campaign.",
        [PatreonCampaignConnectHandler.ResultTaken] = "That campaign is already connected to another account.",
        [PatreonCampaignConnectHandler.ResultLimit] =
            "Your account already has a campaign connected. Disconnect it first to connect a different one.",
        [PatreonCampaignConnectHandler.ResultApiError] = "Patreon returned an error, please try again later.",
        [PatreonCampaignConnectHandler.ResultImportFailed] =
            "Your campaign was connected but importing its members failed. You can retry below.",
        [PatreonCampaignConnectHandler.ResultNotConfigured] = "Connecting campaigns is not available right now."
    };

    private readonly SpaceUserManager _userManager;
    private readonly ApplicationDbContext _db;
    private readonly PatreonImporter _importer;
    private readonly PatreonMembershipManager _membershipManager;
    private readonly ILogger<ManagePatreonCampaigns> _logger;

    public ManagePatreonCampaigns(
        SpaceUserManager userManager,
        ApplicationDbContext db,
        PatreonImporter importer,
        PatreonMembershipManager membershipManager,
        ILogger<ManagePatreonCampaigns> logger)
    {
        _userManager = userManager;
        _db = db;
        _importer = importer;
        _membershipManager = membershipManager;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)] public string Result { get; set; }

    [BindProperty] public List<TierInput> Tiers { get; set; } = new();

    public sealed class TierInput
    {
        public string TierId { get; set; }
        public string Reward { get; set; }
        public bool Enabled { get; set; }
    }

    [TempData] public string StatusMessage { get; set; }

    public List<CampaignRow> Campaigns { get; private set; } = new();

    public sealed record CampaignRow(PatreonCampaign Campaign, int Members);

    public string ResultMessage => Result != null && ResultMessages.TryGetValue(Result, out var m) ? m : null;

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        Campaigns = await _db.PatreonCampaigns
            .Where(c => c.OwnerUserId == user.Id)
            .OrderBy(c => c.Name)
            .Select(c => new CampaignRow(c, c.Memberships.Count))
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostConnectAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        if (await _db.PatreonCampaigns.AnyAsync(c => c.OwnerUserId == user.Id))
        {
            return RedirectToPage(new { result = PatreonCampaignConnectHandler.ResultLimit });
        }

        return Challenge(new AuthenticationProperties
        {
            Items =
            {
                ["SS14UserId"] = user.Id.ToString(),
            },
            RedirectUri = Url.Page("./ManagePatreonCampaigns")
        }, "PatreonCreator");
    }

    public async Task<IActionResult> OnPostRetryImportAsync(int id)
    {
        var campaign = await GetOwnedCampaignAsync(id);
        if (campaign == null)
            return NotFound();

        try
        {
            var count = await _importer.ImportAsync(campaign);
            campaign.Enabled = true;
            await _db.SaveChangesAsync();
            StatusMessage = $"Imported {count} members.";
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Retrying import of Patreon campaign {Campaign} failed", campaign.Key);
            StatusMessage = "Error: importing members failed.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSaveTiersAsync(int id)
    {
        var campaign = await GetOwnedCampaignAsync(id);
        if (campaign == null)
            return NotFound();

        var tierMap = campaign.TierMap;
        var tierNames = campaign.TierNames;
        var disabledTierIds = new HashSet<string>(campaign.DisabledTierIds);
        foreach (var input in Tiers)
        {
            // The tier IDs come from the form, only accept ones the campaign has.
            if (input.TierId == null || !tierMap.ContainsKey(input.TierId))
                continue;

            if (input.Enabled)
                disabledTierIds.Remove(input.TierId);
            else
                disabledTierIds.Add(input.TierId);

            var reward = input.Reward?.Trim();
            if (string.IsNullOrEmpty(reward) || reward.Length > MaxRewardLength)
            {
                StatusMessage = $"Error: reward names must be 1 to {MaxRewardLength} characters.";
                return RedirectToPage();
            }

            (tierMap, tierNames) = PatreonTierMapping.WithReward(tierMap, tierNames, input.TierId, reward);
        }

        campaign.TierMap = tierMap;
        campaign.TierNames = tierNames;
        campaign.DisabledTierIds = disabledTierIds.ToArray();
        await _membershipManager.RemapTiersAsync(campaign);

        StatusMessage = "Tiers saved.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRefreshTiersAsync(int id)
    {
        var campaign = await GetOwnedCampaignAsync(id);
        if (campaign == null)
            return NotFound();

        try
        {
            var added = await _importer.RefreshTiersAsync(campaign);
            StatusMessage = added == 0 ? "No new tiers found." : $"Added {added} new tiers.";
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Refreshing tiers of Patreon campaign {Campaign} failed", campaign.Key);
            StatusMessage = "Error: refreshing tiers failed.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRegenerateTokenAsync(int id)
    {
        var campaign = await GetOwnedCampaignAsync(id);
        if (campaign == null)
            return NotFound();

        var token = PatreonServerToken.Generate();
        campaign.ServerTokenHash = PatreonServerToken.Hash(token);
        await _db.SaveChangesAsync();

        StatusMessage = $"New server token, it will not be shown again: {token}";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDisconnectAsync(int id)
    {
        var campaign = await GetOwnedCampaignAsync(id);
        if (campaign == null)
            return NotFound();

        _db.PatreonCampaigns.Remove(campaign);
        await _db.SaveChangesAsync();

        StatusMessage = "Campaign disconnected. You can also remove the webhook on Patreon.";

        return RedirectToPage();
    }

    private async Task<PatreonCampaign> GetOwnedCampaignAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return null;

        return await _db.PatreonCampaigns.SingleOrDefaultAsync(c => c.Id == id && c.OwnerUserId == user.Id);
    }
}
