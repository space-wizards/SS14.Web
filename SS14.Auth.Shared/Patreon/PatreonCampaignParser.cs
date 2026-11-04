#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SS14.Auth.Shared.Patreon;

/// <summary>
/// A campaign as returned by the Patreon API. <paramref name="Tiers"/> maps tier IDs to titles.
/// </summary>
public sealed record PatreonCampaignInfo(string Id, string Name, Dictionary<string, string> Tiers);

public static class PatreonCampaignParser
{
    public static List<PatreonCampaignInfo> ParseCampaigns(JsonElement root)
    {
        var tierTitles = new Dictionary<string, string>();
        var creatorNames = new Dictionary<string, string>();
        if (root.TryGetProperty("included", out var included) && included.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in included.EnumerateArray())
            {
                if (!item.TryGetProperty("type", out var type)
                    || !item.TryGetProperty("id", out var id) || id.GetString() is not { } idStr
                    || !item.TryGetProperty("attributes", out var attributes))
                {
                    continue;
                }

                if (type.GetString() == "tier" && GetString(attributes, "title") is { } title)
                    tierTitles[idStr] = title;
                else if (type.GetString() == "user" && GetString(attributes, "full_name") is { } fullName)
                    creatorNames[idStr] = fullName;
            }
        }

        var campaigns = new List<PatreonCampaignInfo>();
        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            return campaigns;

        foreach (var campaign in data.EnumerateArray())
        {
            if (!campaign.TryGetProperty("id", out var idProp) || idProp.GetString() is not { } campaignId)
                continue;

            string? name = null; // This can be null in the response for whatever reason
            JsonElement relationships = default;
            var hasRelationships = campaign.TryGetProperty("relationships", out relationships);
            if (hasRelationships
                && relationships.TryGetProperty("creator", out var creatorProp)
                && creatorProp.TryGetProperty("data", out var creatorData)
                && creatorData.ValueKind == JsonValueKind.Object
                && creatorData.TryGetProperty("id", out var creatorId)
                && creatorId.GetString() is { } creatorIdStr)
            {
                creatorNames.TryGetValue(creatorIdStr, out name);
            }

            if (string.IsNullOrWhiteSpace(name) && campaign.TryGetProperty("attributes", out var attributes))
                name = GetString(attributes, "vanity");

            var tiers = new Dictionary<string, string>();
            if (hasRelationships
                && relationships.TryGetProperty("tiers", out var tiersProp)
                && tiersProp.TryGetProperty("data", out var tiersData)
                && tiersData.ValueKind == JsonValueKind.Array)
            {
                foreach (var tier in tiersData.EnumerateArray())
                {
                    if (tier.TryGetProperty("id", out var tierId)
                        && tierId.GetString() is { } tierIdStr
                        && tierTitles.TryGetValue(tierIdStr, out var tierTitle))
                    {
                        tiers[tierIdStr] = tierTitle;
                    }
                }
            }

            campaigns.Add(new PatreonCampaignInfo(
                campaignId,
                string.IsNullOrWhiteSpace(name) ? FallbackName(campaignId) : name,
                tiers));
        }

        return campaigns;
    }

    public static string FallbackName(string campaignId)
    {
        return $"Campaign {campaignId}";
    }

    private static string? GetString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }
}
