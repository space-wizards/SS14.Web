using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using IdentityServer4.EntityFramework.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Areas.Identity.Pages.Account.Manage.OAuthApps;

[ValidateAntiForgeryToken]
public class Create : PageModel
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<SpaceUser> _userManager;

    [BindProperty] public InputModel Input { get; set; }

    public sealed class InputModel
    {
        [Required]
        [DisplayName("Application name")]
        public string Name { get; set; }

        [Required]
        [DisplayName("Homepage URL")]
        public string HomepageUrl { get; set; }

        [Required]
        [DisplayName("Authorization callback URL")]
        public string CallbackUrl { get; set; }
    }

    public Create(ApplicationDbContext dbContext, UserManager<SpaceUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        var client = new Client
        {
            ClientName = Input.Name,
            ClientId = Guid.NewGuid().ToString(),
            AllowedGrantTypes = new List<ClientGrantType>
            {
                new() { GrantType = "authorization_code" }
            },
            AllowedScopes = new List<ClientScope>
            {
                new() { Scope = "openid" },
                new() { Scope = "profile" },
                new() { Scope = "email" }
            },
            AllowedIdentityTokenSigningAlgorithms = "PS256",
            RedirectUris = new List<ClientRedirectUri>
            {
                new() { RedirectUri = Input.CallbackUrl }
            },
            ClientUri = Input.HomepageUrl,
            RequireConsent = true
        };

        var userClient = new UserOAuthClient
        {
            SpaceUser = user,
            Client = client
        };

        _dbContext.Clients.Add(client);
        _dbContext.UserOAuthClients.Add(userClient);

        await _dbContext.SaveChangesAsync();

        return RedirectToPage("Manage", new { client = userClient.UserOAuthClientId });
    }
}