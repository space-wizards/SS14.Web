using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Areas.Admin.Pages.Patreon;

public class Index : PageModel
{
    private readonly ApplicationDbContext _dbContext;

    public List<CampaignRow> Campaigns { get; set; }

    public Index(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public sealed record CampaignRow(PatreonCampaign Campaign, int Members);

    public async Task OnGetAsync()
    {
        var campaigns = await _dbContext.PatreonCampaigns
            .OrderBy(c => c.Name)
            .Select(c => new CampaignRow(c, c.Memberships.Count))
            .ToListAsync();

        Campaigns = campaigns;
    }
}
