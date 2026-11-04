#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SS14.Auth.Shared.Data;

public sealed class PatreonMembership
{
    public long Id { get; set; }

    public int CampaignId { get; set; }

    /// <summary>
    /// Matches <see cref="Patron.PatreonId"/>.
    /// </summary>
    [Required] public string PatreonUserId { get; set; } = default!;

    [Required] public string PatreonMemberId { get; set; } = default!;

    /// <summary>
    /// active_patron, declined_patron, former_patron or null
    /// </summary>
    public string? PatronStatus { get; set; }

    public string[] EntitledTierIds { get; set; } = [];

    /// <summary>
    /// Reward name from the campaign's tier map for the entitled tiers, if any.
    /// </summary>
    public string? CurrentTier { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// If set, the player chose not to share this membership with the campaign's servers
    /// </summary>
    public bool Hidden { get; set; }

    [JsonIgnore]
    public PatreonCampaign Campaign { get; set; } = null!;
}
