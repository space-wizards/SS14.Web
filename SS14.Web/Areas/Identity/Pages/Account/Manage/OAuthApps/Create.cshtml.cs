#nullable enable
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;
using SS14.Auth.Shared.Data;
using SS14.Web.Models;
using SS14.Web.OpenId.Services;

namespace SS14.Web.Areas.Identity.Pages.Account.Manage.OAuthApps;

[ValidateAntiForgeryToken]
public class Create : PageModel
{
    private readonly UserManager<SpaceUser> _userManager;
    private readonly SpaceApplicationManager _appManager;

    [BindProperty] public InputModel Input { get; set; }  = null!;

    public sealed class InputModel
    {
        [Required]
        [DisplayName("Application name")]
        public string Name { get; set; } = null!;

        //[Url(ErrorMessage = "The Homepage URL field is not a valid fully-qualified http or https URL.")]
        [Required]
        [SpaceUrl]
        [DisplayName("Homepage URL")]
        public string HomepageUrl { get; set; }  = null!;

        //[Url(ErrorMessage = "The Authorization callback URL field is not a valid fully-qualified http or https URL.")]
        [Required]
        [SpaceUrl]
        [DisplayName("Authorization callback URL")]
        public string CallbackUrl { get; set; }  = null!;
    }

    public Create(UserManager<SpaceUser> userManager, SpaceApplicationManager appManager)
    {
        _userManager = userManager;
        _appManager = appManager;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await _userManager.GetUserAsync(User);
        var appDescriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = Guid.NewGuid().ToString(),
            ClientSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(36)),
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            ConsentType = OpenIddictConstants.ConsentTypes.Explicit,
            DisplayName = Input.Name,
            RedirectUris = { new Uri(Input.CallbackUrl) },
            Requirements = { OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange },
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
        app.SpaceUserId = user!.Id;
        app.WebsiteUrl = Input.HomepageUrl;
        await _appManager.UpdateAsync(app);

        TempData["ShowSecret"] = 0;
        TempData["ShowSecretValue"] = appDescriptor.ClientSecret;
        return RedirectToPage("Manage", new { client = app.Id });
    }
}
