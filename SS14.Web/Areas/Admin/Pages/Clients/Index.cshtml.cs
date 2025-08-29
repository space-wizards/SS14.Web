#nullable enable
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;
using SS14.Auth.Shared.Data;
using SS14.Web.Extensions;
using SS14.Web.OpenId;
using SS14.Web.OpenId.Services;

namespace SS14.Web.Areas.Admin.Pages.Clients;

public class Index : PageModel
{
    private readonly SpaceApplicationManager _appManager;
    private readonly SpaceUserManager _userManager;

    public IEnumerable<SpaceApplication> Apps { get; set; }

    public Index(SpaceApplicationManager appManager, SpaceUserManager userManager)
    {
        _appManager = appManager;
        _userManager = userManager;
    }

    public async Task OnGetAsync()
    {
        Apps = await _appManager.ListAsync().ToListAsync();
    }

    public async Task<IActionResult> OnPostNewClientAsync()
    {

        var appDescriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = Guid.NewGuid().ToString(),
            ClientSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(36)),
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            ConsentType = OpenIddictConstants.ConsentTypes.Explicit,
            DisplayName = "New Client",
            Requirements = { OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange },
            Settings = {{OpenIdConstants.AllowPlainPkce, "true"}},
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.Introspection,
                OpenIddictConstants.Permissions.Endpoints.EndSession,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Roles,
            },
        };

        var app = await _appManager.CreateAsync(appDescriptor);

        TempData["ShowSecret"] = 0;
        TempData["ShowSecretValue"] = appDescriptor.ClientSecret;
        return RedirectToPage("./Client", new { id = app.Id });
    }


    public async Task<string?> GetUserNameAsync(Guid userId)
    {
        return (await _userManager.FindByIdAsync(userId.ToString()))?.UserName;
    }
}
