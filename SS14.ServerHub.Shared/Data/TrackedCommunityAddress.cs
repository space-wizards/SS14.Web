using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace SS14.ServerHub.Shared.Data;

/// <summary>
/// Represents a single IP address range associated with a <see cref="TrackedCommunity"/>.
/// </summary>
public class TrackedCommunityAddress
{
    /// <summary>
    /// The ID of this entity in the database.
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// The address range in question.
    /// </summary>
    [Column(TypeName = "inet")]
    public (IPAddress, int) Address { get; set; }

    /// <summary>
    /// The ID of the <see cref="TrackedCommunity"/> this address belongs to.
    /// </summary>
    public int TrackedCommunityId { get; set; }
    
    // Navigation properties
    public TrackedCommunity TrackedCommunity { get; set; } = default!;
}