#nullable enable
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Areas.Admin.Pages.Clients;

// TODO: Replace identityserver4 code in this file

public class ConfirmDelete : PageModel
{
    private readonly ApplicationDbContext _dbContext;

    public ConfirmDelete(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    //public DbClient DbClient { get; set; }
    //public string Title => DbClient.ClientName ?? DbClient.ClientId;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        /*DbClient = await _dbContext.Clients.FirstOrDefaultAsync(c => c.Id == id);

        if (DbClient == null)
        {
            return NotFound("Unknown client");
        }*/

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        /*DbClient = await _dbContext.Clients.FirstOrDefaultAsync(c => c.Id == id);

        if (DbClient == null)
        {
            return NotFound("Unknown client");
        }

        _dbContext.Clients.Remove(DbClient);

        await _dbContext.SaveChangesAsync();

        TempData["StatusMessage"] = "OAuth client deleted";*/
        return RedirectToPage("./Index");
    }
}
