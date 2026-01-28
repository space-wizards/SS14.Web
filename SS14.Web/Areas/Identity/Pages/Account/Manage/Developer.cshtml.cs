#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SS14.Auth.Shared.Data;
using SS14.Web.OpenId.Extensions;
using SS14.Web.OpenId.Services;

namespace SS14.Web.Areas.Identity.Pages.Account.Manage;

public class Developer : PageModel
{
    private readonly UserManager<SpaceUser> _userManager;
    private readonly SpaceApplicationManager _appManager;

    public List<SpaceApplication> Apps { get; set; } = [];

    public Developer(UserManager<SpaceUser> userManager, SpaceApplicationManager appManager)
    {
        _userManager = userManager;
        _appManager = appManager;
    }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        Apps = await _appManager.FindApplicationsByUserId(user!.Id);
    }
}
