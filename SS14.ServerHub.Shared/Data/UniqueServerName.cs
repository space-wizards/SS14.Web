namespace SS14.ServerHub.Shared.Data;

// NOTE: This table gets filled in via an update trigger on AdvertisedServer.
public sealed class UniqueServerName
{
    public int AdvertisedServerId { get; set; }
    public string Name { get; set; } = default!;

    public AdvertisedServer AdvertisedServer { get; set; } = default!;
}