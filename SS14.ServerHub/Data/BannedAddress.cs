using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace SS14.ServerHub.Data;

/// <summary>
/// Represents a banned IP address range.
/// </summary>
public class BannedAddress
{
    public int BannedAddressId { get; set; }
    
    /// <summary>
    /// Banned address range.
    /// </summary>
    [Column(TypeName = "inet")]
    public (IPAddress, int) Address { get; set; }

    /// <summary>
    /// Reason for ban.
    /// </summary>
    public string Reason { get; set; } = default!;
}