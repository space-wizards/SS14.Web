using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.ServerHub.Shared.Data;
using SS14.Web.Helpers;

namespace SS14.Web.Areas.Admin.Pages.Servers.Communities;

public sealed class View : PageModel
{
    private readonly HubDbContext _dbContext;

    [BindProperty] public InputModel Input { get; set; }
    [BindProperty] public AddAddressModel AddAddress { get; set; } = new();
    [BindProperty] public AddDomainModel AddDomain { get; set; } = new();

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

    public sealed class InputModel
    {
        public string Name { get; set; }
        public string Notes { get; set; }
        public bool IsBanned { get; set; }
    }
    
    public View(HubDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<IActionResult> OnGetAsync(int id)
    {
        Community = await _dbContext.TrackedCommunity
            .Include(c => c.Addresses)
            .Include(c => c.Domains)
            .SingleOrDefaultAsync(u => u.Id == id);
        
        if (Community == null)
            return NotFound("Community not found");

        Input = new InputModel
        {
            Name = Community.Name,
            IsBanned = Community.IsBanned,
            Notes = Community.Notes
        };
        
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(int id)
    {
        Community = await _dbContext.TrackedCommunity.SingleOrDefaultAsync(u => u.Id == id);
        if (Community == null)
            return NotFound("Community not found");

        Community.Name = Input.Name ?? "";
        Community.Notes = Input.Notes ?? "";
        Community.IsBanned = Input.IsBanned;

        await _dbContext.SaveChangesAsync();

        StatusMessage = "Changed saved";

        return RedirectToPage(new { id = Community.Id });
    }
    
    public async Task<IActionResult> OnPostAddAddressAsync(int id)
    {
        Community = await _dbContext.TrackedCommunity.SingleOrDefaultAsync(c => c.Id == id);
        if (Community == null)
            return NotFound("Community not found");

        if (!IPHelper.TryParseIpOrCidr(AddAddress.Address, out var cidr))
        {
            StatusMessage = "Error: Invalid IP/CIDR";
            return RedirectToPage(new { id = Community.Id });
        }

        _dbContext.TrackedCommunityAddress.Add(new TrackedCommunityAddress
        {
            Address = cidr, TrackedCommunityId = id
        });

        await _dbContext.SaveChangesAsync();
        
        StatusMessage = "Address added";

        return RedirectToPage(new { id = Community.Id });
    }
    
    public async Task<IActionResult> OnPostDeleteAddressAsync(int address)
    {
        var addressEnt = await _dbContext.TrackedCommunityAddress.SingleOrDefaultAsync(c => c.Id == address);
        if (addressEnt == null)
            return NotFound("Address not found");

        _dbContext.TrackedCommunityAddress.Remove(addressEnt);

        await _dbContext.SaveChangesAsync();

        StatusMessage = "Address removed";

        return RedirectToPage(new { id = addressEnt.TrackedCommunityId });
    }
        
    public async Task<IActionResult> OnPostAddDomainAsync(int id)
    {
        Community = await _dbContext.TrackedCommunity.SingleOrDefaultAsync(c => c.Id == id);
        if (Community == null)
            return NotFound("Community not found");

        if (string.IsNullOrWhiteSpace(AddDomain.Domain))
        {
            StatusMessage = "Error: Invalid domain";
            return RedirectToPage(new { id = Community.Id });
        }
        
        _dbContext.TrackedCommunityDomain.Add(new TrackedCommunityDomain
        {
            DomainName = AddDomain.Domain,
            TrackedCommunityId = id
        });

        await _dbContext.SaveChangesAsync();
        
        StatusMessage = "Domain added";

        return RedirectToPage(new { id = Community.Id });
    }
    
    public async Task<IActionResult> OnPostDeleteDomainAsync(int domain)
    {
        var domainEnt = await _dbContext.TrackedCommunityDomain.SingleOrDefaultAsync(c => c.Id == domain);
        if (domainEnt == null)
            return NotFound("Domain not found");

        _dbContext.TrackedCommunityDomain.Remove(domainEnt);

        await _dbContext.SaveChangesAsync();

        StatusMessage = "Domain removed";

        return RedirectToPage(new { id = domainEnt.TrackedCommunityId });
    }
}