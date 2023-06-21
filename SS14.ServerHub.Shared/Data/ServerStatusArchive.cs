using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace SS14.ServerHub.Shared.Data;

public sealed class ServerStatusArchive
{
    public int AdvertisedServerId { get; set; }
    public int ServerStatusArchiveId { get; set; }
    
    /// <summary>
    /// Time when this advertisement was made.
    /// </summary>
    public DateTime Time { get; set; }

    [Column(TypeName = "jsonb")] public byte[] StatusData { get; set; } = default!;
    
    /// <summary>
    /// IP address of the client doing the advertise request. Not actually related to the advertised data.
    /// </summary>
    public IPAddress? AdvertiserAddress { get; set; } 
    
    public AdvertisedServer AdvertisedServer { get; set; } = default!;
}