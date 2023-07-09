using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.ServerHub.Shared.Data;

namespace SS14.Web.Areas.Admin.Pages.Servers.Communities;

public class ConfirmDelete : PageModel
{
    private readonly HubDbContext _dbContext;

    public TrackedCommunity Community { get; private set; }
    
    public ConfirmDelete(HubDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<IActionResult> OnGetAsync(int id)
    {
        Community = await _dbContext.TrackedCommunity.SingleOrDefaultAsync(u => u.Id == id);
        if (Community == null)
            return NotFound();

        return Page();
    }
    
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        Community = await _dbContext.TrackedCommunity.SingleOrDefaultAsync(u => u.Id == id);
        if (Community == null)
            return NotFound();

        _dbContext.TrackedCommunity.Remove(Community);

        await _dbContext.SaveChangesAsync();
        
        TempData["StatusMessage"] = "Community deleted";
        return RedirectToPage("./Index");
    }
}