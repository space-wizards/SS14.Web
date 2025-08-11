#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SS14.ServerHub.Shared.Data;
using SS14.Web.Helpers;

namespace SS14.Web.Areas.Admin.Pages.Servers;

public class AuditLog : PageModel
{
    private readonly HubDbContext _dbContext;

    public PaginationState<AuditEntry> Pagination { get; } = new(100);
    public Dictionary<string, string> AllRouteData { get; } = new();

    public AuditLog(HubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task OnGetAsync(int? pageIndex, int? perPage)
    {
        Pagination.Init(pageIndex, perPage, AllRouteData);

        IQueryable<HubAudit> query = _dbContext.HubAudit;

        query = query.OrderByDescending(x => x.Time);

        await Pagination.LoadLinqAsync(
            query,
            audits => audits.Select(x => new AuditEntry(x, HubAuditEntry.Deserialize(x.Type, x.Data))));
    }

    public sealed record AuditEntry(HubAudit Audit, HubAuditEntry Entry);
}