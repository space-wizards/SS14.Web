using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SS14.ServerHub.Data;

public sealed class AdvertisedServer
{
    public int AdvertisedServerId { get; set; }
        
    /// <summary>
    /// The address of the server. Must be ss14:// or ss14s:// URI.
    /// </summary>
    public string Address { get; set; } = default!;
        
    /// <summary>
    /// The time at which this advertisement will stop showing up.
    /// </summary>
    public DateTime Expires { get; set; } = default!;

    /// <summary>
    /// Last status data polled from the server.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public byte[]? StatusData { get; set; }
}