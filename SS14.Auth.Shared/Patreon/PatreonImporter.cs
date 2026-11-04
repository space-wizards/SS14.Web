#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SS14.Auth.Shared.Data;

namespace SS14.Auth.Shared.Patreon;

public sealed class PatreonImporter
{
    private static readonly TimeSpan TokenRefreshMargin = TimeSpan.FromMinutes(5);

    private readonly ApplicationDbContext _db;
    private readonly PatreonApiClient _api;
    private readonly PatreonSecretProtector _secretProtector;
    private readonly PatreonMembershipManager _membershipManager;

    public PatreonImporter(
        ApplicationDbContext db,
        PatreonApiClient api,
        PatreonSecretProtector secretProtector,
        PatreonMembershipManager membershipManager)
    {
        _db = db;
        _api = api;
        _secretProtector = secretProtector;
        _membershipManager = membershipManager;
    }

    public async Task<int> ImportAsync(PatreonCampaign campaign, CancellationToken cancel = default)
    {
        var accessToken = await GetAccessTokenAsync(campaign, cancel);

        var members = new List<PatreonMemberData>();
        string? cursor = null;
        do
        {
            var page = await _api.GetMembersPageAsync(accessToken, campaign.PatreonCampaignId, cursor, cancel);
            members.AddRange(page.Members);
            cursor = page.NextCursor;
        } while (cursor != null);

        await _membershipManager.ReplaceAllAsync(campaign, members);

        campaign.LastFullSyncAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancel);

        return members.Count;
    }

    /// <summary>
    /// Adds tiers the creator created on Patreon since we last looked to the tier map.
    /// </summary>
    public async Task<int> RefreshTiersAsync(PatreonCampaign campaign, CancellationToken cancel = default)
    {
        var accessToken = await GetAccessTokenAsync(campaign, cancel);

        var campaigns = await _api.GetCampaignsAsync(accessToken, cancel);
        var info = campaigns.Find(c => c.Id == campaign.PatreonCampaignId);
        if (info == null)
            return 0;

        // If we failed to get any sort of name prior, this should hopefully get one  now
        if (campaign.Name == PatreonCampaignParser.FallbackName(campaign.PatreonCampaignId))
            campaign.Name = info.Name;

        var (tierMap, tierNames, added) = PatreonTierMapping.WithNewTiers(
            campaign.TierMap,
            campaign.TierNames,
            info.Tiers);

        if (added > 0)
        {
            campaign.TierMap = tierMap;
            campaign.TierNames = tierNames;
            await _membershipManager.RemapTiersAsync(campaign);
        }

        await _db.SaveChangesAsync(cancel);

        return added;
    }

    private async Task<string> GetAccessTokenAsync(PatreonCampaign campaign, CancellationToken cancel)
    {
        if (campaign.AccessToken == null)
            throw new InvalidOperationException("Campaign has no access token.");

        var expiring = campaign.TokenExpiresAt is { } expiry && expiry - DateTimeOffset.UtcNow < TokenRefreshMargin;
        if (expiring && campaign.RefreshToken != null)
        {
            var tokens = await _api.RefreshTokenAsync(_secretProtector.Unprotect(campaign.RefreshToken), cancel);
            campaign.AccessToken = _secretProtector.Protect(tokens.AccessToken);
            campaign.RefreshToken = _secretProtector.Protect(tokens.RefreshToken);
            campaign.TokenExpiresAt = tokens.ExpiresAt;
            await _db.SaveChangesAsync(cancel);
        }

        return _secretProtector.Unprotect(campaign.AccessToken);
    }
}
