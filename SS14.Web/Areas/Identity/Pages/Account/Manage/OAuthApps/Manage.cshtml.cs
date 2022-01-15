using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Areas.Identity.Pages.Account.Manage.OAuthApps;

public class Manage : PageModel
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<SpaceUser> _userManager;

    public UserOAuthClient App { get; set; }

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
    }

    public Manage(ApplicationDbContext dbContext, UserManager<SpaceUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGetAsync(int client)
    {
        if (await GetAppAndVerifyAccess(client) is { } err)
            return err;

        Input = new InputModel
        {
            Name = App.Client.ClientName,
            CallbackUrl = App.Client.RedirectUris.FirstOrDefault()?.RedirectUri ?? "",
            HomepageUrl = App.Client.ClientUri,
            RequirePkce = App.Client.RequirePkce
        };

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync(int client)
    {
        if (await GetAppAndVerifyAccess(client) is { } err)
            return err;

        App.Client.ClientName = Input.Name;
        App.Client.RedirectUris = new List<ClientRedirectUri> { new() { RedirectUri = Input.CallbackUrl } };
        App.Client.ClientUri = Input.HomepageUrl;
        App.Client.RequirePkce = Input.RequirePkce;

        await _dbContext.SaveChangesAsync();

        return RedirectToPage(new { client });
    }

    public async Task<IActionResult> OnPostCreateSecretAsync(int client)
    {
        if (await GetAppAndVerifyAccess(client) is { } err)
            return err;

        var secretVal = Convert.ToBase64String(RandomNumberGenerator.GetBytes(36));
        
        var secret = new ClientSecret
        {
            Created = DateTime.UtcNow,
            Type = "SharedSecret",
            Description = $"*****{secretVal[^6..]}",
            Value = secretVal.Sha256()
        };
        
        App.Client.ClientSecrets ??= new List<ClientSecret>();
        App.Client.ClientSecrets.Add(secret);

        await _dbContext.SaveChangesAsync();

        ShowSecret = secret.Id;
        ShowSecretValue = secretVal;
        
        return RedirectToPage(new { client });
    }
    
    public async Task<IActionResult> OnPostDeleteSecretAsync(int client, int secret)
    {
        if (await GetAppAndVerifyAccess(client) is { } err)
            return err;

        var dbSecret = App.Client.ClientSecrets.Find(p => p.Id == secret);
        if (dbSecret == null)
            return NotFound("Secret not found");

        _dbContext.ClientSecrets.Remove(dbSecret);

        await _dbContext.SaveChangesAsync();
        
        return RedirectToPage(new {id = client});
    }

    // Null return indicates everything good.
    private async Task<IActionResult> GetAppAndVerifyAccess(int client)
    {
        var user = await _userManager.GetUserAsync(User);
        App = await _dbContext.UserOAuthClients
            .Include(c => c.Client)
            .ThenInclude(c => c.RedirectUris)
            .Include(c => c.Client)
            .ThenInclude(c => c.ClientSecrets)
            .SingleOrDefaultAsync(oa => oa.UserOAuthClientId == client);

        if (App == null)
            return NotFound();

        if (!VerifyAppAccess(user, App))
            return Forbid();

        return null;
    }

    public static bool VerifyAppAccess(
        SpaceUser user,
        UserOAuthClient userClient)
    {
        return user == userClient.SpaceUser;
    }
}