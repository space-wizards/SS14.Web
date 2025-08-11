#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Areas.Admin.Pages.Users;

public class ConfirmDelete : PageModel
{
    private readonly UserManager<SpaceUser> _userManager;

    public ConfirmDelete(UserManager<SpaceUser> userManager)
    {
        _userManager = userManager;
    }

    public SpaceUser SpaceUser { get; set; }
        
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        SpaceUser = await _userManager.FindByIdAsync(id.ToString());

        if (SpaceUser == null)
        {
            return NotFound("Unknown user");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        SpaceUser = await _userManager.FindByIdAsync(id.ToString());

        if (SpaceUser == null)
        {
            return NotFound("Unknown user");
        }

        await _userManager.DeleteAsync(SpaceUser);

        TempData["StatusMessage"] = "User deleted";
        return RedirectToPage("./Index");
    }
}