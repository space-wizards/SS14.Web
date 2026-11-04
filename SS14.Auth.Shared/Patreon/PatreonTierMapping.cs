#nullable enable
using System.Collections.Generic;

namespace SS14.Auth.Shared.Patreon;

/// <summary>
/// Edits to a campaign's tier map. These return new dictionaries, because EF doesn't notice changes made to the
/// dictionaries of a jsonb column in place.
/// </summary>
public static class PatreonTierMapping
{
    /// <summary>
    /// Sets the reward name of a tier. The tier keeps showing under its old display name, unless the new reward
    /// already has one.
    /// </summary>
    public static (Dictionary<string, string> TierMap, Dictionary<string, string> TierNames) WithReward(
        IReadOnlyDictionary<string, string> tierMap,
        IReadOnlyDictionary<string, string> tierNames,
        string tierId,
        string reward)
    {
        var newMap = new Dictionary<string, string>(tierMap);
        var newNames = new Dictionary<string, string>(tierNames);

        var oldReward = newMap.GetValueOrDefault(tierId);
        var displayName = oldReward == null ? reward : newNames.GetValueOrDefault(oldReward, oldReward);

        newMap[tierId] = reward;
        newNames.TryAdd(reward, displayName);

        return (newMap, newNames);
    }

    public static (Dictionary<string, string> TierMap, Dictionary<string, string> TierNames, int Added) WithNewTiers(
        IReadOnlyDictionary<string, string> tierMap,
        IReadOnlyDictionary<string, string> tierNames,
        IReadOnlyDictionary<string, string> tiers)
    {
        var newMap = new Dictionary<string, string>(tierMap);
        var newNames = new Dictionary<string, string>(tierNames);

        var added = 0;
        foreach (var (id, title) in tiers)
        {
            if (!newMap.TryAdd(id, title))
                continue;

            newNames.TryAdd(title, title);
            added += 1;
        }

        return (newMap, newNames, added);
    }
}
