using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SS14.Auth.Shared.Patreon;

namespace SS14.Auth.Shared.Data;

public sealed class PatreonDataManager
{
    private readonly ApplicationDbContext _db;

    public PatreonDataManager(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<string> GetPatreonTierAsync(SpaceUser user)
    {
        return await (
                from patron in _db.Patrons
                where patron.SpaceUserId == user.Id
                join membership in _db.PatreonMemberships on patron.PatreonId equals membership.PatreonUserId
                where membership.Campaign.IsFirstParty && membership.Campaign.Enabled
                select membership.CurrentTier)
            .FirstOrDefaultAsync();
    }

    public async Task<PatreonCampaign> GetCampaignByServerTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        var hash = PatreonServerToken.Hash(token);
        return await _db.PatreonCampaigns.SingleOrDefaultAsync(c => c.Enabled && c.ServerTokenHash == hash);
    }

    /// <summary>
    /// Gets the memberships of the given users in a campaign, leaving out users who hide it from the campaign's
    /// servers. Users without a (visible) membership are not in the result.
    /// </summary>
    public async Task<Dictionary<Guid, PatreonCampaignStatus>> GetCampaignStatusesAsync(
        PatreonCampaign campaign,
        IReadOnlyCollection<Guid> userIds)
    {
        var rows = await (
                from patron in _db.Patrons
                where userIds.Contains(patron.SpaceUserId)
                join membership in _db.PatreonMemberships on patron.PatreonId equals membership.PatreonUserId
                where membership.CampaignId == campaign.Id && !membership.Hidden
                select new { patron.SpaceUserId, membership.PatronStatus, membership.CurrentTier })
            .ToListAsync();

        return rows.ToDictionary(r => r.SpaceUserId, r => new PatreonCampaignStatus(r.PatronStatus, r.CurrentTier));
    }
}

public sealed record PatreonCampaignStatus(string PatronStatus, string Tier);
