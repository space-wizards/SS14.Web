namespace SS14.ServerHub.Shared.Data;

public sealed class TrackedCommunityDomain
{
    public int Id { get; set; }

    /// <summary>
    /// Banned domain name (all subdomains will be included).
    /// </summary>
    public string DomainName { get; set; } = default!;

    public int TrackedCommunityId { get; set; }
    public TrackedCommunity TrackedCommunity { get; set; } = default!;
}