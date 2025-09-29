namespace SS14.ServerHub.Shared.Data;

/// <summary>
/// Organizes a known community with specific information such as their IP addresses and domains.
/// </summary>
public sealed class TrackedCommunity
{
    /// <summary>
    /// ID of this entity in the database.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The name of this community, as displayed throughout the UI to admins.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Any useful notes admins may want to store about this community.
    /// </summary>
    public string Notes { get; set; } = default!;

    /// <summary>
    /// The time this community was created by an admin.
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// The last time any information for this community was updated by an admin.
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// This community is banned, and server advertisements should be disallowed.
    /// </summary>
    public bool IsBanned { get; set; }

    /// <summary>
    /// This community is except from only advertising a limited amount of servers from one IP address
    /// </summary>
    public bool IsExceptFromMaxAdvertisements { get; set; }

    // Navigation properties
    public List<TrackedCommunityAddress> Addresses { get; set; } = default!;
    public List<TrackedCommunityDomain> Domains { get; set; } = default!;
    public List<TrackedCommunityInfoMatch> InfoMatches { get; set; } = default!;
}
