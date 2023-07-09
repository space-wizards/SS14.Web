using System.Net;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using SS14.ServerHub.Shared.Data;

namespace SS14.ServerHub.Shared;

/// <summary>
/// Shared functionality for matching tracked communities against servers.
/// </summary>
public static class CommunityMatcher
{
    public static IQueryable<TrackedCommunityAddress> CheckIP(HubDbContext dbContext, IPAddress address)
    {
        return dbContext.TrackedCommunityAddress
            .Where(c => EF.Functions.ContainsOrEqual(c.Address, address));
    }
    
    public static IQueryable<TrackedCommunityDomain> CheckDomain(HubDbContext dbContext, string domain)
    {
        return dbContext.TrackedCommunityDomain
            .Where(d => d.DomainName == domain || EF.Functions.Like(domain, "%." + d.DomainName));
    }

    /// <summary>
    /// Find all communities that match the given server address.
    /// </summary>
    /// <param name="dbContext">Database context to look up in.</param>
    /// <param name="address">The server address to look up.</param>
    /// <param name="communities">List to write results into.</param>
    /// <exception cref="FailedResolveException">
    /// Thrown if <paramref name="address"/> is a domain that failed to resolve.
    /// If this happens, <paramref name="communities"/> may still contain some results.
    /// </exception>
    public static async Task MatchCommunities(HubDbContext dbContext, Uri address, List<TrackedCommunity> communities)
    {
        var host = address.Host;

        if (!IPAddress.TryParse(host, out _))
        {
            // If a domain name, check for domain ban.
            var domains = await CheckDomain(dbContext, host)
                .Include(b => b.TrackedCommunity)
                .Select(b => b.TrackedCommunity)
                .ToArrayAsync();

            communities.AddRange(domains);
        }

        IPAddress[] addresses;
        try
        {
            // If the host is an IP address, GetHostAddressesAsync returns it directly.
            addresses = await Dns.GetHostAddressesAsync(host);
        }
        catch (SocketException e)
        {
            throw new FailedResolveException("Failed to resolve domain", e);
        }

        // Check EVERY address.
        foreach (var checkAddress in addresses)
        {
            var addressBan = await CheckIP(dbContext, checkAddress)
                .Include(b => b.TrackedCommunity)
                .Select(b => b.TrackedCommunity)
                .ToArrayAsync();

            communities.AddRange(addressBan);
        }
    }

    public sealed class FailedResolveException : Exception
    {
        public FailedResolveException(string message, Exception e) : base(message, e)
        {
            
        }
    }
}