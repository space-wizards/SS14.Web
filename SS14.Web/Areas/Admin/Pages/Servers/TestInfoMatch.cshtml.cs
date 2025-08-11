using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SS14.ServerHub.Shared.Data;

namespace SS14.Web.Areas.Admin.Pages.Servers;

public class TestInfoMatch : PageModel
{
    private readonly HubDbContext _dbContext;

    public string CurrentPath { get; set; }
    public InfoMatchField CurrentField { get; set; }

    public List<ServerEntry> Servers { get; } = new();

    [TempData] public string StatusMessage { get; set; }

    public TestInfoMatch(HubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task OnGetAsync(string path, InfoMatchField field)
    {
        CurrentPath = path;
        CurrentField = field;

        if (string.IsNullOrWhiteSpace(path))
            return;

        IQueryable<AdvertisedServer> statusMatch;
        switch (CurrentField)
        {
            case InfoMatchField.Status:
                statusMatch = _dbContext.AdvertisedServer.FromSqlInterpolated($$"""
                    SELECT *
                    FROM   public."AdvertisedServer"
                    WHERE  jsonb_path_exists("StatusData", {{path}}::jsonpath, '{}', true)
                    """);
                break;
            case InfoMatchField.Info:
                statusMatch = _dbContext.AdvertisedServer.FromSqlInterpolated($$"""
                    SELECT *
                    FROM   public."AdvertisedServer"
                    WHERE  jsonb_path_exists("InfoData", {{path}}::jsonpath, '{}', true)
                    """);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        try
        {
            await ListActive.DownloadEntriesForDisplay(statusMatch, Servers);
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.SyntaxError)
        {
            // Thrown if the user entered an invalid JSON path.

            StatusMessage = $"Error: invalid path syntax: {e.Message}";
        }
    }
}