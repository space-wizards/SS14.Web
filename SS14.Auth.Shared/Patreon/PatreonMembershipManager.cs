#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SS14.Auth.Shared.Data;

namespace SS14.Auth.Shared.Patreon;

public sealed class PatreonMembershipManager
{
    private readonly ApplicationDbContext _db;

    public PatreonMembershipManager(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task UpsertAsync(PatreonCampaign campaign, PatreonMemberData data)
    {
        return WithRetryOnConflictAsync(async () =>
        {
            var membership = await _db.PatreonMemberships
                .SingleOrDefaultAsync(m => m.CampaignId == campaign.Id && m.PatreonUserId == data.UserId);

            if (membership == null)
            {
                membership = new PatreonMembership
                {
                    CampaignId = campaign.Id,
                    PatreonUserId = data.UserId
                };
                _db.PatreonMemberships.Add(membership);
            }

            Apply(membership, campaign, data);

            await _db.SaveChangesAsync();
        });
    }

    /// <summary>
    /// Makes the campaign's memberships match the given list exactly, removing the ones not in it.
    /// </summary>
    public Task ReplaceAllAsync(PatreonCampaign campaign, IReadOnlyCollection<PatreonMemberData> members)
    {
        return WithRetryOnConflictAsync(async () =>
        {
            var existing = await _db.PatreonMemberships
                .Where(m => m.CampaignId == campaign.Id)
                .ToDictionaryAsync(m => m.PatreonUserId);

            var seen = new HashSet<string>();
            foreach (var data in members)
            {
                seen.Add(data.UserId);

                if (!existing.TryGetValue(data.UserId, out var membership))
                {
                    membership = new PatreonMembership
                    {
                        CampaignId = campaign.Id,
                        PatreonUserId = data.UserId
                    };
                    _db.PatreonMemberships.Add(membership);
                    existing.Add(data.UserId, membership);
                }

                Apply(membership, campaign, data);
            }

            _db.PatreonMemberships.RemoveRange(existing.Where(e => !seen.Contains(e.Key)).Select(e => e.Value));

            await _db.SaveChangesAsync();
        });
    }

    public static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }

    /// <summary>
    /// Runs a write, and once more if it lost a race with another writer inserting the same membership.
    /// If a webhook request comes in while running a large sync, this would trigger
    /// </summary>
    private async Task WithRetryOnConflictAsync(Func<Task> write)
    {
        try
        {
            await write();
        }
        catch (DbUpdateException e) when (IsUniqueViolation(e))
        {
            foreach (var entry in _db.ChangeTracker.Entries<PatreonMembership>().ToList())
            {
                entry.State = EntityState.Detached;
            }

            await write();
        }
    }

    /// <summary>
    /// Recomputes the stored tier of the campaign's memberships, after its tier map changed
    /// </summary>
    public async Task RemapTiersAsync(PatreonCampaign campaign)
    {
        var memberships = await _db.PatreonMemberships
            .Where(m => m.CampaignId == campaign.Id)
            .ToListAsync();

        foreach (var membership in memberships)
        {
            membership.CurrentTier = MapTier(campaign.TierMap, membership.EntitledTierIds, campaign.DisabledTierIds);
        }

        await _db.SaveChangesAsync();
    }

    public async Task RemoveAsync(PatreonCampaign campaign, string patreonUserId)
    {
        var memberships = await _db.PatreonMemberships
            .Where(m => m.CampaignId == campaign.Id && m.PatreonUserId == patreonUserId)
            .ToListAsync();

        _db.PatreonMemberships.RemoveRange(memberships);
        await _db.SaveChangesAsync();
    }

    public static string? MapTier(
        IReadOnlyDictionary<string, string> tierMap,
        IEnumerable<string> tierIds,
        IReadOnlyCollection<string>? disabledTierIds = null)
    {
        return tierIds
            .Where(t => tierMap.ContainsKey(t) && disabledTierIds?.Contains(t) != true)
            .Select(t => tierMap[t])
            .FirstOrDefault();
    }

    private static void Apply(PatreonMembership membership, PatreonCampaign campaign, PatreonMemberData data)
    {
        membership.PatreonMemberId = data.MemberId;
        membership.PatronStatus = data.PatronStatus;
        membership.EntitledTierIds = data.TierIds;
        membership.CurrentTier = MapTier(campaign.TierMap, data.TierIds, campaign.DisabledTierIds);
        membership.UpdatedAt = DateTimeOffset.UtcNow;
    }
}
