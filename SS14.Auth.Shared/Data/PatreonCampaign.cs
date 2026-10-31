#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SS14.Auth.Shared.Data;

public sealed class PatreonCampaign
{
    public int Id { get; set; }

    /// <summary>
    /// Short unique name used in URLs referencing this campaign
    /// </summary>
    [Required] public string Key { get; set; } = default!;

    [Required] public string PatreonCampaignId { get; set; } = default!;

    [Required] public string Name { get; set; } = default!;

    /// <summary>
    /// Maps Patreon tier IDs to reward names
    /// </summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, string> TierMap { get; set; } = new();

    /// <summary>
    /// Maps reward names to display names
    /// </summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, string> TierNames { get; set; } = new();

    /// <summary>
    /// Patreon tier IDs that are turned off
    /// </summary>
    public string[] DisabledTierIds { get; set; } = [];

    // The following secrets are encrypted, see PatreonSecretProtector
    public string? WebhookSecret { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTimeOffset? TokenExpiresAt { get; set; }

    /// <summary>
    /// Hash of the token the campaign's servers use to query memberships
    /// </summary>
    public string? ServerTokenHash { get; set; }

    /// <summary>
    /// If set, this campaign provides the patron tier returned by the query API and shown on the account page.
    /// At most one campaign can be first party. This replicates the previous system
    /// </summary>
    public bool IsFirstParty { get; set; }

    /// <summary>
    /// The user who connected the campaign and manages it. Null for campaigns added by admins
    /// </summary>
    public Guid? OwnerUserId { get; set; }

    /// <summary>
    /// Campaigns connected by their creator stay disabled until the first import has finished.
    /// Admins can also disable a campaign should the need arise
    /// </summary>
    public bool Enabled { get; set; }
    public DateTimeOffset? LastFullSyncAt { get; set; }

    /// <summary>
    /// When Patreon last sent a valid webhook for this campaign. Helps spot a broken webhook
    /// </summary>
    public DateTimeOffset? LastWebhookAt { get; set; }

    public List<PatreonMembership> Memberships { get; set; } = default!;
}
