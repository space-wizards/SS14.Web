using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SS14.ServerHub.Shared.Data;
using SS14.ServerHub.Shared.Helpers;
using SS14.Web.Data;

namespace SS14.Web.Areas.Admin.Pages.Servers.Communities;

public sealed class View : PageModel
{
    private readonly HubDbContext _dbContext;
    private readonly HubAuditLogManager _hubAuditLog;

    [BindProperty] public InputModel Input { get; set; }
    [BindProperty] public AddAddressModel AddAddress { get; set; } = new();
    [BindProperty] public AddDomainModel AddDomain { get; set; } = new();
    [BindProperty] public AddInfoMatchModel AddInfoMatch { get; set; } = new();

    [TempData] public string StatusMessage { get; set; }

    public TrackedCommunity Community { get; private set; }

    public sealed class AddAddressModel
    {
        public string Address { get; set; }
    }

    public sealed class AddDomainModel
    {
        public string Domain { get; set; }
    }

    public sealed class AddInfoMatchModel
    {
        public InfoMatchField Field { get; set; }
        public string Path { get; set; }
    }

    public sealed class InputModel
    {
        public string Name { get; set; }
        public string Notes { get; set; }
        public bool IsBanned { get; set; }
        public bool IsExceptFromMaxAdvertisements  { get; set; }
    }

    public View(HubDbContext dbContext, HubAuditLogManager hubAuditLog)
    {
        _dbContext = dbContext;
        _hubAuditLog = hubAuditLog;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Community = await _dbContext.TrackedCommunity
            .Include(c => c.Addresses)
            .Include(c => c.Domains)
            .Include(c => c.InfoMatches)
            .AsSplitQuery()
            .SingleOrDefaultAsync(u => u.Id == id);

        if (Community == null)
            return NotFound("Community not found");

        Input = new InputModel
        {
            Name = Community.Name,
            IsBanned = Community.IsBanned,
            IsExceptFromMaxAdvertisements = Community.IsExceptFromMaxAdvertisements,
            Notes = Community.Notes
        };

        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(int id)
    {
        Community = await _dbContext.TrackedCommunity.SingleOrDefaultAsync(u => u.Id == id);
        if (Community == null)
            return NotFound("Community not found");

        var inputName = (Input.Name ?? "").Trim();
        var inputNotes = Input.Notes ?? "";

        var anyChange = false;

        if (Community.Name != inputName)
        {
            _hubAuditLog.Log(User, new HubAuditCommunityChangedName(Community, Community.Name, inputName));
            Community.Name = inputName;
            anyChange = true;
        }

        if (Community.Notes != inputNotes)
        {
            _hubAuditLog.Log(User, new HubAuditCommunityChangedNotes(Community, Community.Notes, inputNotes));
            Community.Notes = inputNotes;
            anyChange = true;
        }

        if (Community.IsBanned != Input.IsBanned)
        {
            _hubAuditLog.Log(User, new HubAuditCommunityChangedBanned(Community, Community.IsBanned, Input.IsBanned));
            Community.IsBanned = Input.IsBanned;
            anyChange = true;
        }

        if (Community.IsExceptFromMaxAdvertisements != Input.IsExceptFromMaxAdvertisements)
        {
            _hubAuditLog.Log(User, new HubAuditCommunityChangedBanned(Community, Community.IsExceptFromMaxAdvertisements, Input.IsExceptFromMaxAdvertisements));
            Community.IsExceptFromMaxAdvertisements = Input.IsExceptFromMaxAdvertisements;
            anyChange = true;
        }

        if (!anyChange)
            return RedirectToPage(new { id = Community.Id });

        Community.LastUpdated = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        StatusMessage = "Changed saved";

        return RedirectToPage(new { id = Community.Id });
    }

    public async Task<IActionResult> OnPostAddAddressAsync(int id)
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync();

        Community = await _dbContext.TrackedCommunity.SingleOrDefaultAsync(c => c.Id == id);
        if (Community == null)
            return NotFound("Community not found");

        if (!IPHelper.TryParseIpOrCidr(AddAddress.Address ?? "", out var cidr))
        {
            StatusMessage = "Error: Invalid IP/CIDR";
            return RedirectToPage(new { id = Community.Id });
        }

        var address = new TrackedCommunityAddress
        {
            Address = cidr, TrackedCommunityId = id
        };

        _dbContext.TrackedCommunityAddress.Add(address);
        Community.LastUpdated = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _hubAuditLog.Log(User, new HubAuditCommunityAddressAdd(Community, address));

        await _dbContext.SaveChangesAsync();

        await tx.CommitAsync();

        StatusMessage = "Address added";

        return RedirectToPage(new { id = Community.Id });
    }

    public async Task<IActionResult> OnPostDeleteAddressAsync(int address)
    {
        var addressEnt = await _dbContext.TrackedCommunityAddress
            .Include(c => c.TrackedCommunity)
            .SingleOrDefaultAsync(c => c.Id == address);

        if (addressEnt == null)
            return NotFound("Address not found");

        _dbContext.TrackedCommunityAddress.Remove(addressEnt);
        _hubAuditLog.Log(User, new HubAuditCommunityAddressDelete(addressEnt.TrackedCommunity, addressEnt));
        addressEnt.TrackedCommunity.LastUpdated = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        StatusMessage = "Address removed";

        return RedirectToPage(new { id = addressEnt.TrackedCommunityId });
    }

    public async Task<IActionResult> OnPostAddDomainAsync(int id)
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync();

        Community = await _dbContext.TrackedCommunity.SingleOrDefaultAsync(c => c.Id == id);
        if (Community == null)
            return NotFound("Community not found");

        if (string.IsNullOrWhiteSpace(AddDomain.Domain))
        {
            StatusMessage = "Error: Invalid domain";
            return RedirectToPage(new { id = Community.Id });
        }

        var domainEnt = new TrackedCommunityDomain
        {
            DomainName = AddDomain.Domain.Trim(),
            TrackedCommunityId = id
        };
        _dbContext.TrackedCommunityDomain.Add(domainEnt);
        Community.LastUpdated = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _hubAuditLog.Log(User, new HubAuditCommunityDomainAdd(Community, domainEnt));

        await _dbContext.SaveChangesAsync();

        await tx.CommitAsync();

        StatusMessage = "Domain added";

        return RedirectToPage(new { id = Community.Id });
    }

    public async Task<IActionResult> OnPostDeleteDomainAsync(int domain)
    {
        var domainEnt = await _dbContext.TrackedCommunityDomain
            .Include(c => c.TrackedCommunity)
            .SingleOrDefaultAsync(c => c.Id == domain);

        if (domainEnt == null)
            return NotFound("Domain not found");

        _dbContext.TrackedCommunityDomain.Remove(domainEnt);
        _hubAuditLog.Log(User, new HubAuditCommunityDomainDelete(domainEnt.TrackedCommunity, domainEnt));
        domainEnt.TrackedCommunity.LastUpdated = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        StatusMessage = "Domain removed";

        return RedirectToPage(new { id = domainEnt.TrackedCommunityId });
    }

    //
    // Info matches
    //

    public async Task<IActionResult> OnPostAddInfoMatchAsync(int id)
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync();

        Community = await _dbContext.TrackedCommunity.SingleOrDefaultAsync(c => c.Id == id);
        if (Community == null)
            return NotFound("Community not found");

        if (string.IsNullOrWhiteSpace(AddInfoMatch.Path))
        {
            StatusMessage = "Error: Path empty";
            return RedirectToPage(new { id = Community.Id });
        }

        TrackedCommunityInfoMatch matchEnt;
        try
        {
            matchEnt = await InsertInfoMatch(tx, AddInfoMatch.Path, AddInfoMatch.Field, id);
        }
        catch (NpgsqlException e) when (e.SqlState == PostgresErrorCodes.SyntaxError)
        {
            StatusMessage = $"Error: invalid path syntax: {e.Message}";
            return RedirectToPage(new { id = Community.Id });
        }

        Community.LastUpdated = DateTime.UtcNow;

        _hubAuditLog.Log(User, new HubAuditCommunityInfoMatchAdd(Community, matchEnt));

        await _dbContext.SaveChangesAsync();

        await tx.CommitAsync();

        StatusMessage = "Info match added";

        return RedirectToPage(new { id = Community.Id });
    }

    public async Task<IActionResult> OnPostDeleteInfoMatchAsync(int match)
    {
        var matchEnt = await _dbContext.TrackedCommunityInfoMatch
            .Include(c => c.TrackedCommunity)
            .SingleOrDefaultAsync(c => c.Id == match);

        if (matchEnt == null)
            return NotFound("Domain not found");

        _dbContext.TrackedCommunityInfoMatch.Remove(matchEnt);
        _hubAuditLog.Log(User, new HubAuditCommunityInfoMatchDelete(matchEnt.TrackedCommunity, matchEnt));
        matchEnt.TrackedCommunity.LastUpdated = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        StatusMessage = "Info match removed";

        return RedirectToPage(new { id = matchEnt.TrackedCommunityId });
    }

    private async Task<TrackedCommunityInfoMatch> InsertInfoMatch(
        IDbContextTransaction transaction,
        string path,
        InfoMatchField field,
        int trackedCommunity)
    {
        // We can't insert jsonpath values via EF Core, so have to do this manually.

        var con = (NpgsqlConnection)_dbContext.Database.GetDbConnection();
        await using var cmd = new NpgsqlCommand("""
            INSERT INTO "TrackedCommunityInfoMatch" ("Path", "Field", "TrackedCommunityId")
            VALUES ($1::jsonpath, $2, $3)
            RETURNING "Id";
            """, con, (NpgsqlTransaction)transaction.GetDbTransaction())
        {
            Parameters =
            {
                new() { Value = path },
                new() { Value = (int)field },
                new() { Value = trackedCommunity },
            }
        };

        var result = (int) (await cmd.ExecuteScalarAsync())!;

        return await _dbContext.TrackedCommunityInfoMatch.SingleAsync(x => x.Id == result);
    }
}
