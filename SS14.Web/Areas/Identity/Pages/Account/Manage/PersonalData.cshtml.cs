#nullable enable
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Areas.Identity.Pages.Account.Manage;

public class PersonalDataModel(
    UserManager<SpaceUser> userManager,
    PersonalDataCollector personalDataCollector)
    : PageModel
{
    public async Task<IActionResult> OnGet()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDownloadPersonalDataAsync(CancellationToken cancel)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        var data = await personalDataCollector.CollectPersonalData(user, cancel);
        Response.Headers.Add("Content-Disposition", $"attachment; filename={user.UserName}-PersonalData.zip");
        return new FileStreamResult(data, MediaTypeNames.Application.Zip);
    }
}
