using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SS14.Auth.Shared.Data;
using SS14.Auth.Shared.Emails;

namespace SS14.Web.Areas.Identity.Pages.Account.Manage;

public class PersonalDataModel(
    UserManager<SpaceUser> userManager,
    IEmailSender emailSender,
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

        // TODO: Once net 9 is in, in order to not hit MaxEmailsPerHour. This should have a rate limit to only allow data download once per hour or 1 email per hour
        // var userEmail = await userManager.GetEmailAsync(user);
        // await emailSender.SendEmailAsync(userEmail,
        //     "Your Space Station 14 account data was requested",
        //     $"This email was sent to you to confirm your account data was requested. If this was you feel free to ignore this email." +
        //     $"\n\nIf this was not you, send an email to support@spacestation14.com immediately.");

        return new FileStreamResult(data, MediaTypeNames.Application.Zip);
    }
}
