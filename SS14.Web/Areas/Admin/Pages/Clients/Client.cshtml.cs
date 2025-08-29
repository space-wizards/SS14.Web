#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;
using SS14.Auth.Shared.Data;
using SS14.Web.Models.Types;
using SS14.Web.OpenId;
using SS14.Web.OpenId.Extensions;
using SS14.Web.OpenId.Services;
using static OpenIddict.Abstractions.OpenIddictConstants.ConsentTypes;

namespace SS14.Web.Areas.Admin.Pages.Clients;

// TODO: Replace identityserver4 code in this file

public class Client : PageModel
{
    private readonly SpaceApplicationManager _appManager;

    public Client(SpaceApplicationManager appManager)
    {
        _appManager = appManager;
    }

    [BindProperty] public InputModel Input { get; set; }
    [BindProperty] public CreateSecretModel CreateSecretInput { get; set; }

    [TempData] public string StatusMessage { get; set; }

    public SpaceApplication? App { get; set; }

    public string? Title => App?.DisplayName ?? App?.ClientId;
    public IEnumerable<ClientSecretInfo> Secrets { get; set; }

    public sealed class CreateSecretModel
    {
        [Display(Name = "Desc")]
        public string? Description { get; set; }

        public string? Value { get; set; }
    }

    public string[] ConsentTypes = [Implicit, Explicit, Systematic];

    // That's a lotta options.
    public sealed class InputModel
    {
        public bool Enabled { get; set; }
        [Display(Name = "Client Id")]
        public string? ClientId { get; set; }
        [Display(Name = "Client Name")]
        public string? ClientName { get; set; }

        // Explicit remembers the consent and Systematic forces a consent check every time
        [Required]
        [Display(Name = "Consent Type")]
        [AllowedValues(Explicit, Implicit, Systematic)]
        public string ConsentType { get; set; } = null!;
        [Display(Name = "Require Pkce")]
        public bool RequirePkce { get; set; }
        public bool AllowPlainTextPkce { get; set; }
        [Display(Name = "Home Page")]
        public string? ClientUri { get; set; }
        [Display(Name = "Logo Uri")]
        public string? LogoUri { get; set; }
        [Display(Name = "Allow Offline Access")]
        public bool AllowOfflineAccess { get; set; }
        [Display(Name = "Identity Token Lifetime")]
        public int IdentityTokenLifetime { get; set; }
        [Display(Name = "Allowed ID Token Signing Alg.")]
        public string? AllowedIdentityTokenSigningAlgorithms { get; set; }
        [Display(Name = "Access Token Lifetime")]
        public int AccessTokenLifetime { get; set; }
        [Display(Name = "Refresh Token Lifetime")]
        public int RefreshTokenLifetime { get; set; }
        [Display(Name = "Redirect URIs (one per line)")]
        public string? RedirectUris { get; set; }

        [Display(Name = "Allowed Scopes (one per line)")]
        public string? AllowedScopes { get; set; }

        [Display(Name = "Allowed Grant Types (one per line)")]
        public string? AllowedGrantTypes { get; set; }

        [Display(Name = "Post Logout Redirect Uris")]
        public string? PostLogoutRedirectUris { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        App = await _appManager.FindByIdAsync(id);
        if (App == null)
            return NotFound("Client not found");

        var requirements = await _appManager.GetRequirementsAsync(App);
        var settings = await _appManager.GetSettingsAsync(App);
        var permissions = await _appManager.GetPermissionsAsync(App);
        var redirectUris = await _appManager.GetRedirectUrisAsync(App);

        var grantTypes = permissions
            .Where(x => x.StartsWith(OpenIddictConstants.Permissions.Prefixes.GrantType))
            .Select(x => x[OpenIddictConstants.Permissions.Prefixes.GrantType.Length..]);

        var scopes = permissions
            .Where(x => x.StartsWith(OpenIddictConstants.Permissions.Prefixes.Scope))
            .Select(x => x[OpenIddictConstants.Permissions.Prefixes.Scope.Length..]);

        Input = new InputModel
        {
            Enabled = !await  _appManager.IsDisabled(App),
            ClientId = App.ClientId,
            ClientName = App.DisplayName,
            ConsentType = App.ConsentType ?? Explicit,
            RequirePkce = requirements.Contains(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange),
            AllowPlainTextPkce = await _appManager.GetAllowPlainPkceSetting(App),
            ClientUri = App.WebsiteUrl,
            LogoUri = App.LogoUri,
            AllowOfflineAccess = permissions.Contains(OpenIddictConstants.Permissions.GrantTypes.RefreshToken),
            AllowedIdentityTokenSigningAlgorithms = settings.GetValueOrDefault(OpenIdConstants.SigningAlgorithmSetting),
            IdentityTokenLifetime = await _appManager.GetIdentityTokenLifetime(App),
            AccessTokenLifetime = await _appManager.GetAccessTokenLifetime(App),
            RefreshTokenLifetime = await _appManager.GetRefreshTokenLifetime(App),
            RedirectUris = string.Join("\n", redirectUris),
            AllowedGrantTypes = string.Join("\n", grantTypes),
            AllowedScopes = string.Join("\n", scopes),
            PostLogoutRedirectUris = App.PostLogoutRedirectUris,
        };

        CreateSecretInput = new CreateSecretModel();

        Secrets = _appManager.ListSecrets(App);

        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(string id)
    {
        var app = await _appManager.FindByIdAsync(id);
        if (app == null)
            return NotFound("Client not found");

        var descriptor = new OpenIddictApplicationDescriptor();
        await _appManager.PopulateAsync(descriptor, app);

        descriptor.ClientId = Input.ClientId;
        descriptor.DisplayName = Input.ClientName;
        descriptor.ConsentType = Input.ConsentType;
        descriptor.Settings[OpenIdConstants.DisabledSetting] = Input.Enabled ? "false"  : "true";
        descriptor.Settings[OpenIdConstants.AllowPlainPkce] = Input.AllowPlainTextPkce ? "true" : "false";
        descriptor.Settings[OpenIdConstants.SigningAlgorithmSetting] = Input.AllowedIdentityTokenSigningAlgorithms ?? string.Empty;

        descriptor.Permissions.RemoveWhere(x => x.StartsWith(OpenIddictConstants.Permissions.Prefixes.GrantType));

        foreach (var grantType in Input.AllowedGrantTypes?.Split("\n", StringSplitOptions.RemoveEmptyEntries) ?? [])
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.GrantType+grantType.Trim());
        }

        descriptor.Permissions.RemoveWhere(x => x.StartsWith(OpenIddictConstants.Permissions.Prefixes.Scope));

        foreach (var scope in Input.AllowedScopes?.Split("\n", StringSplitOptions.RemoveEmptyEntries) ?? [])
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope+scope.Trim());
        }

        if (Input.RequirePkce)
            descriptor.Requirements.Add(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);
        else
            descriptor.Requirements.Remove(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);

        if (Input.AllowOfflineAccess)
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);
        else
            descriptor.Permissions.Remove(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);

        await _appManager.UpdateAsync(app, descriptor);
        return RedirectToPage(new {id});
    }

    public async Task<IActionResult> OnPostCreateSecretAsync(int id)
    {
        /*DbClient = await _dbContext.Clients
            .Include(c => c.RedirectUris)
            .SingleOrDefaultAsync(c => c.Id == id);

        if (DbClient == null)
        {
            return NotFound("Client not found");
        }

        var value = CreateSecretInput.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            var bytes = new byte[24];
            var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            value = Convert.ToBase64String(bytes);
        }

        var secret = new ClientSecret
        {
            Description = CreateSecretInput.Description,
            Type = CreateSecretInput.Type,
            Value = value.Sha256()
        };

        DbClient.ClientSecrets ??= new List<ClientSecret>();
        DbClient.ClientSecrets.Add(secret);

        await _dbContext.SaveChangesAsync();

        StatusMessage = $"Secret created. Value: {value}";

        return RedirectToPage(new {id});*/
        return NotFound();
    }

    public async Task<IActionResult> OnPostDeleteSecretAsync(int secret)
    {
        /*var dbSecret = await _dbContext.ClientSecrets
            .SingleOrDefaultAsync(c => c.Id == secret);

        if (dbSecret == null)
        {
            return NotFound("Secret not found");
        }

        _dbContext.ClientSecrets.Remove(dbSecret);

        await _dbContext.SaveChangesAsync();

        StatusMessage = "Secret deleted.";

        return RedirectToPage(new {id = dbSecret.ClientId});*/
        return NotFound();
    }
}
