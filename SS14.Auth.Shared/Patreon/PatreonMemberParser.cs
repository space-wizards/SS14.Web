#nullable enable
using System.Collections.Generic;
using System.Text.Json;

namespace SS14.Auth.Shared.Patreon;

public sealed record PatreonMemberData(
    string MemberId,
    string UserId,
    string? PatronStatus,
    string[] TierIds);

/// <summary>
/// One page of a campaign's member list.
/// <paramref name="NextCursor"/> is null on the last page
/// </summary>
public sealed record PatreonMemberPage(IReadOnlyList<PatreonMemberData> Members, string? NextCursor);

public static class PatreonMemberParser
{
    public static PatreonMemberData? ParseWebhook(JsonElement root)
    {
        if (!root.TryGetProperty("data", out var data))
            return null;

        return ParseMember(data);
    }

    public static PatreonMemberPage ParsePage(JsonElement root)
    {
        var members = new List<PatreonMemberData>();
        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in data.EnumerateArray())
            {
                if (ParseMember(item) is { } member)
                    members.Add(member);
            }
        }

        string? next = null;
        if (root.TryGetProperty("meta", out var meta)
            && meta.TryGetProperty("pagination", out var pagination)
            && pagination.TryGetProperty("cursors", out var cursors)
            && cursors.TryGetProperty("next", out var nextProp)
            && nextProp.ValueKind == JsonValueKind.String)
        {
            next = nextProp.GetString();
        }

        return new PatreonMemberPage(members, next);
    }

    /// <summary>
    /// Parses a member resource, as found in webhooks and campaign member lists.
    /// Returns null if the member has no user to link it to
    /// </summary>
    public static PatreonMemberData? ParseMember(JsonElement member)
    {
        if (member.ValueKind != JsonValueKind.Object || !member.TryGetProperty("id", out var idProp))
            return null;

        if (!member.TryGetProperty("relationships", out var relationships))
            return null;

        var userId = GetSingleId(relationships, "user");
        if (userId == null)
            return null;

        string? status = null;
        if (member.TryGetProperty("attributes", out var attributes)
            && attributes.TryGetProperty("patron_status", out var statusProp)
            && statusProp.ValueKind == JsonValueKind.String)
        {
            status = statusProp.GetString();
        }

        var tiers = new List<string>();
        if (relationships.TryGetProperty("currently_entitled_tiers", out var tiersProp)
            && tiersProp.TryGetProperty("data", out var tiersData)
            && tiersData.ValueKind == JsonValueKind.Array)
        {
            foreach (var tier in tiersData.EnumerateArray())
            {
                if (tier.TryGetProperty("id", out var tierId) && tierId.GetString() is { } tierIdStr)
                    tiers.Add(tierIdStr);
            }
        }

        return new PatreonMemberData(idProp.GetString()!, userId, status, tiers.ToArray());
    }

    private static string? GetSingleId(JsonElement relationships, string name)
    {
        if (!relationships.TryGetProperty(name, out var rel)
            || !rel.TryGetProperty("data", out var data)
            || data.ValueKind != JsonValueKind.Object
            || !data.TryGetProperty("id", out var id))
        {
            return null;
        }

        return id.GetString();
    }
}
