using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.ServerHub.Shared.Data;
using SS14.Web.Data;
using SS14.Web.Helpers;

#nullable enable

namespace SS14.Web.Areas.Admin.Pages.Servers.Communities;

public sealed class Index : PageModel
{
    private readonly HubDbContext _dbContext;
    private readonly HubAuditLogManager _hubAuditLog;

    public PaginationState<TrackedCommunity> Pagination { get; } = new(100);
    public SortState<TrackedCommunity> SortState { get; } = new();
    public Dictionary<string, string?> AllRouteData { get; } = new();

    public string? CurrentFilter { get; set; } = "";

    public Index(HubDbContext dbContext, HubAuditLogManager hubAuditLog)
    {
        _dbContext = dbContext;
        _hubAuditLog = hubAuditLog;
    }
    
    public async Task OnGetAsync(string? sort,
        string? search,
        int? pageIndex,
        int? perPage)
    {
        SortState.AddColumn("name", p => p.Name, SortOrder.Ascending);
        SortState.AddColumn("created", p => p.Created);
        SortState.AddColumn("updated", p => p.LastUpdated);
        SortState.AddColumn("banned", p => p.IsBanned);
        SortState.Init(sort, AllRouteData);

        Pagination.Init(pageIndex, perPage, AllRouteData);
        
        CurrentFilter = search?.Trim();
        AllRouteData.Add("search", CurrentFilter);

        IQueryable<TrackedCommunity> query = _dbContext.TrackedCommunity;
        
        query = FilterQuery(query, CurrentFilter);

        query = SortState.ApplyToQuery(query);

        await Pagination.LoadAsync(query);
    }

    public async Task<IActionResult> OnPostNewCommunityAsync()
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync();
        
        var community = new TrackedCommunity
        {
            Name = "change this",
            Notes = ""
        };
        
        // ReSharper disable once MethodHasAsyncOverload
        _dbContext.TrackedCommunity.Add(community);
        
        await _dbContext.SaveChangesAsync();

        _hubAuditLog.Log(User, new HubAuditCommunityCreated(community));
        
        await _dbContext.SaveChangesAsync();

        await tx.CommitAsync();
        
        return RedirectToPage("./View", new { id = community.Id });
    }
    
    private static IQueryable<TrackedCommunity> FilterQuery(IQueryable<TrackedCommunity> query, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return query;

        Expression<Func<TrackedCommunity, bool>> expr = c => EF.Functions.ILike(c.Name, $"%{filter}%");
        return query.Where(expr);
    }
}