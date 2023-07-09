namespace SS14.ServerHub.Shared.Data;

/// <summary>
/// Organizes a known community with specific information such as their IP addresses and domains.
/// </summary>
public sealed class TrackedCommunity
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Notes { get; set; } = default!;
    public DateTime Created { get; set; }
    public DateTime LastUpdated { get; set; }
    
    /// <summary>
    /// This community is banned, and server advertisements should be disallowed.
    /// </summary>
    public bool IsBanned { get; set; }

    public List<TrackedCommunityAddress> Addresses { get; set; } = default!;
    public List<TrackedCommunityDomain> Domains { get; set; } = default!;
}