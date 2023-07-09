namespace SS14.ServerHub.Shared.Data;

/// <summary>
/// Represents a single domain name associated with a <see cref="TrackedCommunity"/>.
/// </summary>
public sealed class TrackedCommunityDomain
{
    /// <summary>
    /// The ID of this entity in the database.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The domain name in question (all subdomains will be included as well).
    /// </summary>
    public string DomainName { get; set; } = default!;

    /// <summary>
    /// The ID of the <see cref="TrackedCommunity"/> this address belongs to.
    /// </summary>
    public int TrackedCommunityId { get; set; }

    // Navigation properties
    public TrackedCommunity TrackedCommunity { get; set; } = default!;
}