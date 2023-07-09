using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace SS14.ServerHub.Shared.Data;

public class TrackedCommunityAddress
{
    public int Id { get; set; }
    
    /// <summary>
    /// address range.
    /// </summary>
    [Column(TypeName = "inet")]
    public (IPAddress, int) Address { get; set; }

    public int TrackedCommunityId { get; set; }
    public TrackedCommunity TrackedCommunity { get; set; } = default!;
}