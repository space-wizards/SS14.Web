#nullable enable

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SS14.Auth.Shared.Data;
using SS14.Web.OpenId.Services;

namespace SS14.Web.Areas.Admin.Pages.Clients;

public class ConfirmDelete : PageModel
{
    private readonly SpaceApplicationManager _applicationManager;

    public ConfirmDelete(SpaceApplicationManager applicationManager)
    {
        _applicationManager = applicationManager;
    }

    public SpaceApplication? App { get; set; }
    public string? Title => App?.DisplayName ?? App?.ClientId;

    public async Task<IActionResult> OnGetAsync(string id)
    {
        App = await _applicationManager.FindByIdAsync(id);
        if (App == null)
            return NotFound("Unknown client");

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var app = await _applicationManager.FindByIdAsync(id);
        if (app == null)
            return NotFound("Unknown client");

        await _applicationManager.DeleteAsync(app);

        TempData["StatusMessage"] = "OAuth client deleted";
        return RedirectToPage("./Index");
    }
}
