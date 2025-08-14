using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Core;
using SS14.Auth.Shared.Data;
using SS14.Web.Extensions;
using SS14.Web.OpenId.Extensions;

namespace SS14.Web.Areas.Identity.Pages.Account.Manage.OAuthApps;

// TODO: Replace identityserver4 code in this file

public class Manage : PageModel
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<SpaceUser> _userManager;
    private readonly OpenIddictApplicationManager<SpaceApplication> _appManager;

    //public UserOAuthClient App { get; set; }
    public SpaceApplication App { get; set; }

    [BindProperty] public InputModel Input { get; set; }

    [TempData] public int ShowSecret { get; set; }
    [TempData] public string ShowSecretValue { get; set; }

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

        [Required]
        [DisplayName("Require PKCE")]
        public bool RequirePkce { get; set; }

        [Required]
        [DisplayName("Allow PS256 signing")]
        public bool AllowPS256 { get; set; } = true;
    }

    public Manage(ApplicationDbContext dbContext, UserManager<SpaceUser> userManager, OpenIddictApplicationManager<SpaceApplication> appManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _appManager = appManager;
    }

    public async Task<IActionResult> OnGetAsync(int client)
    {
        if (await GetAppAndVerifyAccess(client) is { } err)
            return err;


        Input = new InputModel
        {
            Name = App.DisplayName,
            CallbackUrl = await _appManager.GetFirstRedirectUrl(App) ?? "",
            HomepageUrl = await _appManager.GetHomePageProperty(App) ?? "",
            RequirePkce = await _appManager.GetRequiresPkceSetting(App),
            //AllowPS256 = App.Client.AllowedIdentityTokenSigningAlgorithms?.Contains("PS256") ?? false
        };

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync(int client)
    {
        if (await GetAppAndVerifyAccess(client) is { } err)
            return err;

        //App.Client.ClientName = Input.Name;
        //App.Client.RedirectUris = new List<ClientRedirectUri> { new() { RedirectUri = Input.CallbackUrl } };
        //App.Client.ClientUri = Input.HomepageUrl;
        //App.Client.RequirePkce = Input.RequirePkce;
        //App.Client.AllowedIdentityTokenSigningAlgorithms = Input.AllowPS256 ? "PS256" : null;

        await _dbContext.SaveChangesAsync();

        return RedirectToPage(new { client });
    }

    public async Task<IActionResult> OnPostCreateSecretAsync(int client)
    {
        if (await GetAppAndVerifyAccess(client) is { } err)
            return err;

        var secretVal = Convert.ToBase64String(RandomNumberGenerator.GetBytes(36));

        /*var secret = new ClientSecret
        {
            Created = DateTime.UtcNow,
            Type = "SharedSecret",
            Description = $"*****{secretVal[^6..]}",
            Value = secretVal.Sha256()
        };*/

        //App.Client.ClientSecrets ??= new List<ClientSecret>();
        //App.Client.ClientSecrets.Add(secret);

        await _dbContext.SaveChangesAsync();

        //ShowSecret = secret.Id;
        ShowSecretValue = secretVal;

        return RedirectToPage(new { client });
    }

    public async Task<IActionResult> OnPostDeleteSecretAsync(int client, int secret)
    {
        if (await GetAppAndVerifyAccess(client) is { } err)
            return err;

        //var dbSecret = App.Client.ClientSecrets.Find(p => p.Id == secret);
        //if (dbSecret == null)
        //    return NotFound("Secret not found");

        //_dbContext.ClientSecrets.Remove(dbSecret);

        await _dbContext.SaveChangesAsync();

        return RedirectToPage(new {id = client});
    }

    // Null return indicates everything good.
    private async Task<IActionResult> GetAppAndVerifyAccess(int client)
    {
        var user = await _userManager.GetUserAsync(User);
        //App = await _dbContext.UserOAuthClients
        //    .Include(c => c.Client)
        //    .ThenInclude(c => c.RedirectUris)
        //    .Include(c => c.Client)
        //    .ThenInclude(c => c.ClientSecrets)
        //    .SingleOrDefaultAsync(oa => oa.UserOAuthClientId == client);

        //if (App == null)
        //    return NotFound();

        //if (!VerifyAppAccess(user, App))
        //    return Forbid();

        return null;
    }

    /*public static bool VerifyAppAccess(
        SpaceUser user,
        UserOAuthClient userClient)
    {
        return user == userClient.SpaceUser;
    }*/
}
