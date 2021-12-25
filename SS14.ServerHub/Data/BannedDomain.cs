namespace SS14.ServerHub.Data;

/// <summary>
/// Represents a banned domain name (and all its subdomains).
/// </summary>
public sealed class BannedDomain
{
    public int BannedDomainId { get; set; }

    /// <summary>
    /// Banned domain name (all subdomains will be included).
    /// </summary>
    public string DomainName { get; set; } = default!;
    
    /// <summary>
    /// Reason for ban.
    /// </summary>
    public string Reason { get; set; } = default!;
}