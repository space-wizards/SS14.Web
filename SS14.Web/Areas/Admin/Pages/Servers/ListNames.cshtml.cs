#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.ServerHub.Shared.Data;
using SS14.Web.Helpers;

namespace SS14.Web.Areas.Admin.Pages.Servers;

public class ListNames : PageModel
{
    public const int PerPage = 100;
    
    private readonly HubDbContext _hubDbContext;

    public string CurrentFilter { get; set; }

    public PaginatedList<UniqueServerName> ServerList { get; set; }

    public ListNames(HubDbContext hubDbContext)
    {
        _hubDbContext = hubDbContext;
    }

    public async Task OnGetAsync(string searchString, int? pageIndex)
    {
        CurrentFilter = searchString;
        
        IQueryable<UniqueServerName> query = _hubDbContext.UniqueServerName;
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            searchString = searchString.Trim().ToUpper();
            query = query.Where(x => x.Name.ToUpper().Contains(searchString));
        }

        query = query.OrderByDescending(x => x.LastSeen);

        query = query.Include(x => x.AdvertisedServer);

        ServerList = await PaginatedList<UniqueServerName>.CreateAsync(query, pageIndex ?? 0, PerPage);
    }
}