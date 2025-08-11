#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.ServerHub.Shared.Data;

namespace SS14.Web.Areas.Admin.Pages.Servers;

public class ListActive : PageModel
{
    private readonly HubDbContext _dbContext;

    public List<ServerEntry> Servers { get; } = new();

    public ListActive(HubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task OnGetAsync()
    {
        await DownloadEntriesForDisplay(_dbContext.AdvertisedServer, Servers);
    }

    public static async Task DownloadEntriesForDisplay(
        IQueryable<AdvertisedServer> query,
        List<ServerEntry> servers)
    {
        var filtered = await query
            .Where(x => x.Expires > DateTime.UtcNow)
            .ToListAsync();

        foreach (var server in filtered)
        {
            var statusData = JsonSerializer.Deserialize<ServerStatusData>(server.StatusData);
            servers.Add(new ServerEntry(
                server.AdvertisedServerId,
                server.Address,
                statusData
            ));
        }

        servers.Sort((a, b) => -a.StatusData.Players.CompareTo(b.StatusData.Players));
    }
}

public static class ServerJson
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
}

public sealed record ServerEntry(
    int Id,
    string Address,
    ServerStatusData StatusData
);

public sealed record ServerStatusData(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("players")]
    int Players,
    [property: JsonPropertyName("soft_max_players")]
    int? SoftMaxPlayers,
    [property: JsonPropertyName("tags")] string[] Tags
);

public sealed record ServerInfoData(
    [property: JsonPropertyName("connect_address")]
    string ConnectAddress,
    [property: JsonPropertyName("auth")] ServerInfoAuth Auth,
    [property: JsonPropertyName("build")] ServerBuildInfo Build,
    [property: JsonPropertyName("desc")] string Desc,
    [property: JsonPropertyName("links")] ServerInfoLink[] Links
);

public sealed record ServerInfoLink(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("icon")] string Icon,
    [property: JsonPropertyName("url")] string Url
);

public sealed record ServerInfoAuth(
    [property: JsonPropertyName("mode")] ServerAuthMode Mode,
    [property: JsonPropertyName("public_key")]
    string PublicKey
);

public enum ServerAuthMode
{
    Required,
    Optional,
    Disabled
}

public sealed record ServerBuildInfo(
);