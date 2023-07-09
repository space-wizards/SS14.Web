using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.ServerHub.Shared;
using SS14.ServerHub.Shared.Data;

namespace SS14.Web.Areas.Admin.Pages.Servers;

public class Server : PageModel
{
    public const int CountNames = 50;
    
    private readonly HubDbContext _hubDbContext;

    public int ServerId { get; private set; }
    public string Address { get; private set; } = "";
    public ServerStatusData Status { get; private set; }
    public ServerInfoData Info { get; private set; }
    public bool Online { get; private set; }

    public UniqueServerName[] UniqueNames { get; private set; }
    public List<TrackedCommunity> MatchedCommunities { get; private set; } = new();

    public Server(HubDbContext hubDbContext)
    {
        _hubDbContext = hubDbContext;
    }

    public async Task<IActionResult> OnGetAsync(int serverId)
    {
        var server = await _hubDbContext.AdvertisedServer.SingleOrDefaultAsync(x => x.AdvertisedServerId == serverId);
        if (server == null)
            return NotFound("Server not found");

        try
        {
            await CommunityMatcher.MatchCommunities(_hubDbContext, new Uri(server.Address), MatchedCommunities);
        }
        catch (CommunityMatcher.FailedResolveException)
        {
            // Meh.
        }
        
        UniqueNames = await _hubDbContext.UniqueServerName
            .Where(u => u.AdvertisedServerId == serverId)
            .OrderByDescending(u => u.LastSeen)
            .Take(CountNames)
            .ToArrayAsync();
        
        if (server.StatusData != null)
        {
            Status = JsonSerializer.Deserialize<ServerStatusData>(server.StatusData, ServerJson.JsonOptions);
        }

        if (server.InfoData != null)
        {
            Info = JsonSerializer.Deserialize<ServerInfoData>(server.InfoData, ServerJson.JsonOptions);
        }
        
        ServerId = serverId;
        Address = server.Address;
        Online = server.Expires > DateTime.UtcNow;

        return Page();
    }
}