using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;
using SS14.Auth.Shared.Data;
using SS14.Web.Models.Types;
using SS14.Web.OpenId;
using SS14.Web.OpenId.Extensions;
using SS14.Web.OpenId.Services;

namespace SS14.Web.Areas.Identity.Pages.Account.Manage.OAuthApps;

public class Manage : PageModel
{
    private readonly UserManager<SpaceUser> _userManager;
    private readonly SpaceApplicationManager _appManager;

    public SpaceApplication App { get; set; }

    [BindProperty] public InputModel Input { get; set; }

    public List<ClientSecretInfo> Secrets { get; set; }

    [TempData] public int? ShowSecret { get; set; }
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

    public Manage(UserManager<SpaceUser> userManager, SpaceApplicationManager appManager)
    {
        _userManager = userManager;
        _appManager = appManager;
    }

    public async Task<IActionResult> OnGetAsync(string client)
    {
        if (await GetAppAndVerifyAccess(client) is { } err)
            return err;

        Input = new InputModel
        {
            Name = App.DisplayName,
            CallbackUrl = await _appManager.GetFirstRedirectUrl(App) ?? "",
            HomepageUrl = App.WebsiteUrl ?? "",
            RequirePkce = await _appManager.GetRequiresPkce(App),
            AllowPS256 = await _appManager.GetPs256Setting(App),
        };

        Secrets = _appManager.ListSecrets(App);

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync(string client)
    {
        if (await GetAppAndVerifyAccess(client) is { } err)
            return err;

        var descriptor = new OpenIddictApplicationDescriptor();
        await _appManager.PopulateAsync(descriptor, App);
        descriptor.DisplayName = Input.Name;
        descriptor.RedirectUris.Clear();
        descriptor.RedirectUris.Add(new Uri(Input.CallbackUrl));

        if (Input.RequirePkce)
        {
            descriptor.Requirements.Add(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);
        }
        else
        {
            descriptor.Requirements.Remove(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);
        }

        descriptor.Settings[OpenIdConstants.SigningAlgorithmSetting] = Input.AllowPS256
            ? OpenIddictConstants.Algorithms.RsaSsaPssSha256
            : null;

        App.WebsiteUrl = Input.HomepageUrl;

        await _appManager.UpdateAsync(App, descriptor);
        return RedirectToPage(new { client });
    }

    public async Task<IActionResult> OnPostCreateSecretAsync(string client)
    {
        if (await GetAppAndVerifyAccess(client) is { } err)
            return err;

        var secretVal = Convert.ToBase64String(RandomNumberGenerator.GetBytes(36));

        var secretInfo = await _appManager.AddSecret(App, secretVal);
        ShowSecret = secretInfo.Id;
        ShowSecretValue = secretVal;

        return RedirectToPage(new { client });
    }

    public async Task<IActionResult> OnPostDeleteSecretAsync(string client, int secret)
    {
        if (await GetAppAndVerifyAccess(client) is { } err)
            return err;

        await _appManager.RemoveSecret(App, secret);

        return RedirectToPage(new {id = client});
    }

    // Null return indicates everything good.
    private async Task<IActionResult> GetAppAndVerifyAccess(string client)
    {
        var user = await _userManager.GetUserAsync(User);

        App = await _appManager.FindByIdAsync(client);
        if (App == null)
            return NotFound();

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (!VerifyAppAccess(user, App))
            return Forbid();

        return null;
    }

    public static bool VerifyAppAccess(
        SpaceUser user,
        SpaceApplication app)
    {
        return user == app.SpaceUser;
    }
}
