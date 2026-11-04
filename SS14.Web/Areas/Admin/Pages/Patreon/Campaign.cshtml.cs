using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SS14.Auth.Shared.Data;
using SS14.Auth.Shared.Patreon;

namespace SS14.Web.Areas.Admin.Pages.Patreon;

public class Campaign : PageModel
{
    private static readonly Regex KeyRegex = new("^[a-z0-9-]+$");

    private readonly ApplicationDbContext _dbContext;
    private readonly PatreonSecretProtector _secretProtector;
    private readonly PatreonImporter _importer;
    private readonly PatreonMembershipManager _membershipManager;
    private readonly ILogger<Campaign> _logger;

    public Campaign(
        ApplicationDbContext dbContext,
        PatreonSecretProtector secretProtector,
        PatreonImporter importer,
        PatreonMembershipManager membershipManager,
        ILogger<Campaign> logger)
    {
        _dbContext = dbContext;
        _secretProtector = secretProtector;
        _importer = importer;
        _membershipManager = membershipManager;
        _logger = logger;
    }

    [BindProperty] public InputModel Input { get; set; }

    [TempData] public string StatusMessage { get; set; }

    public PatreonCampaign DbCampaign { get; set; }
    public int MemberCount { get; set; }

    public string Title => DbCampaign?.Name ?? "New campaign";

    public sealed class InputModel
    {
        [Display(Name = "Key (lowercase letters, numbers and dashes)")]
        public string Key { get; set; }

        public string Name { get; set; }

        [Display(Name = "Patreon campaign ID")]
        public string PatreonCampaignId { get; set; }

        [Display(Name = "First party")]
        public bool IsFirstParty { get; set; }

        public bool Enabled { get; set; }

        [Display(Name = "Tier map (one per line, tier ID=reward)")]
        public string TierMap { get; set; }

        [Display(Name = "Tier names (one per line, reward=display name)")]
        public string TierNames { get; set; }

        [Display(Name = "Disabled tier IDs (one per line)")]
        public string DisabledTierIds { get; set; }

        [Display(Name = "Webhook secret (leave empty to keep)")]
        public string WebhookSecret { get; set; }

        [Display(Name = "Access token (leave empty to keep)")]
        public string AccessToken { get; set; }

        [Display(Name = "Refresh token (leave empty to keep)")]
        public string RefreshToken { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            Input = new InputModel { Enabled = true };
            return Page();
        }

        if (!await LoadAsync(id.Value))
            return NotFound("Campaign not found");

        Input = new InputModel
        {
            Key = DbCampaign.Key,
            Name = DbCampaign.Name,
            PatreonCampaignId = DbCampaign.PatreonCampaignId,
            IsFirstParty = DbCampaign.IsFirstParty,
            Enabled = DbCampaign.Enabled,
            TierMap = FormatPairs(DbCampaign.TierMap),
            TierNames = FormatPairs(DbCampaign.TierNames),
            DisabledTierIds = string.Join("\n", DbCampaign.DisabledTierIds)
        };

        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(int? id)
    {
        if (id == null)
        {
            DbCampaign = new PatreonCampaign();
        }
        else if (!await LoadAsync(id.Value))
        {
            return NotFound("Campaign not found");
        }

        if (!KeyRegex.IsMatch(Input.Key ?? ""))
            ModelState.AddModelError("Input.Key", "Invalid key.");

        if (string.IsNullOrWhiteSpace(Input.Name))
            ModelState.AddModelError("Input.Name", "Name is required.");

        if (string.IsNullOrWhiteSpace(Input.PatreonCampaignId))
            ModelState.AddModelError("Input.PatreonCampaignId", "Campaign ID is required.");

        if (!TryParsePairs(Input.TierMap, out var tierMap))
            ModelState.AddModelError("Input.TierMap", "Invalid tier map.");

        if (!TryParsePairs(Input.TierNames, out var tierNames))
            ModelState.AddModelError("Input.TierNames", "Invalid tier names.");

        var otherCampaigns = _dbContext.PatreonCampaigns.Where(c => c.Id != DbCampaign.Id);

        if (await otherCampaigns.AnyAsync(c => c.Key == Input.Key))
            ModelState.AddModelError("Input.Key", "Key is already in use.");

        if (await otherCampaigns.AnyAsync(c => c.PatreonCampaignId == Input.PatreonCampaignId))
            ModelState.AddModelError("Input.PatreonCampaignId", "Campaign is already added.");

        if (Input.IsFirstParty && await otherCampaigns.AnyAsync(c => c.IsFirstParty))
            ModelState.AddModelError("Input.IsFirstParty", "Another campaign is already first party.");

        if (!ModelState.IsValid)
        {
            MemberCount = await _dbContext.PatreonMemberships.CountAsync(m => m.CampaignId == DbCampaign.Id);
            return Page();
        }

        DbCampaign.Key = Input.Key;
        DbCampaign.Name = Input.Name;
        DbCampaign.PatreonCampaignId = Input.PatreonCampaignId;
        DbCampaign.IsFirstParty = Input.IsFirstParty;
        DbCampaign.Enabled = Input.Enabled;
        DbCampaign.TierMap = tierMap;
        DbCampaign.TierNames = tierNames;
        DbCampaign.DisabledTierIds = (Input.DisabledTierIds ?? "")
            .Replace("\r", "")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .ToArray();

        if (!string.IsNullOrEmpty(Input.WebhookSecret))
            DbCampaign.WebhookSecret = _secretProtector.Protect(Input.WebhookSecret);

        if (!string.IsNullOrEmpty(Input.AccessToken))
        {
            DbCampaign.AccessToken = _secretProtector.Protect(Input.AccessToken);
            // Expiry is unknown for pasted tokens, so they are never refreshed automatically.
            DbCampaign.TokenExpiresAt = null;
        }

        if (!string.IsNullOrEmpty(Input.RefreshToken))
            DbCampaign.RefreshToken = _secretProtector.Protect(Input.RefreshToken);

        if (id == null)
            _dbContext.PatreonCampaigns.Add(DbCampaign);

        await _dbContext.SaveChangesAsync();

        // Memberships store their mapped tier, so they need updating when the map changed.
        await _membershipManager.RemapTiersAsync(DbCampaign);

        StatusMessage = "Changes saved";

        return RedirectToPage(new { id = DbCampaign.Id });
    }

    public async Task<IActionResult> OnPostImportAsync(int id)
    {
        if (!await LoadAsync(id))
            return NotFound("Campaign not found");

        try
        {
            var count = await _importer.ImportAsync(DbCampaign);
            StatusMessage = $"Imported {count} members";
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Importing members of Patreon campaign {Campaign} failed", DbCampaign.Key);
            StatusMessage = $"Error: import failed ({e.Message})";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostGenerateTokenAsync(int id)
    {
        if (!await LoadAsync(id))
            return NotFound("Campaign not found");

        var token = PatreonServerToken.Generate();
        DbCampaign.ServerTokenHash = PatreonServerToken.Hash(token);
        await _dbContext.SaveChangesAsync();

        StatusMessage = $"Server token generated, it will not be shown again. Value: {token}";

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (!await LoadAsync(id))
            return NotFound("Campaign not found");

        _dbContext.PatreonCampaigns.Remove(DbCampaign);
        await _dbContext.SaveChangesAsync();

        TempData["StatusMessage"] = "Campaign deleted";

        return RedirectToPage("./Index");
    }

    private async Task<bool> LoadAsync(int id)
    {
        DbCampaign = await _dbContext.PatreonCampaigns.SingleOrDefaultAsync(c => c.Id == id);
        if (DbCampaign == null)
            return false;

        MemberCount = await _dbContext.PatreonMemberships.CountAsync(m => m.CampaignId == id);
        return true;
    }

    private static string FormatPairs(Dictionary<string, string> pairs)
    {
        return string.Join("\n", pairs.Select(p => $"{p.Key}={p.Value}"));
    }

    private static bool TryParsePairs(string text, out Dictionary<string, string> pairs)
    {
        pairs = new Dictionary<string, string>();

        foreach (var line in (text ?? "").Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var split = line.Split('=', 2);
            if (split.Length != 2 || split[0].Trim() == "" || split[1].Trim() == "")
                return false;

            pairs[split[0].Trim()] = split[1].Trim();
        }

        return true;
    }
}
