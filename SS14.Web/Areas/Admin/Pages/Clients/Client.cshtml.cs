#nullable enable
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Areas.Admin.Pages.Clients;

// TODO: Replace identityserver4 code in this file

public class Client : PageModel
{
    private readonly ApplicationDbContext _dbContext;

    public Client(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [BindProperty] public InputModel Input { get; set; }
    [BindProperty] public CreateSecretModel CreateSecretInput { get; set; }

    [TempData] public string StatusMessage { get; set; }

    //public DbClient DbClient { get; set; }

    //public string Title => DbClient.ClientName ?? DbClient.ClientId;

    //public IEnumerable<ClientSecret> Secrets { get; set; }

    public sealed class CreateSecretModel
    {
        [Display(Name = "Desc")]
        public string Description { get; set; }
        public string Value { get; set; }
        public string Type { get; set; } = "SharedSecret";
    }

    // That's a lotta options.
    public sealed class InputModel
    {
        public bool Enabled { get; set; }
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public string ProtocolType { get; set; }
        public bool RequireClientSecret { get; set; }
        public bool RequireConsent { get; set; }
        public bool AllowRememberConsent { get; set; }
        public bool AlwaysIncludeUserClaimsInIdToken { get; set; }
        public bool RequirePkce { get; set; }
        public bool AllowPlainTextPkce { get; set; }
        public bool RequireRequestObject { get; set; }
        public string Description { get; set; }
        public string ClientUri { get; set; }
        public string LogoUri { get; set; }
        public bool AllowAccessTokensViaBrowser { get; set; }
        public string FrontChannelLogoutUri { get; set; }
        public bool FrontChannelLogoutSessionRequired { get; set; }
        public string BackChannelLogoutUri { get; set; }
        public bool BackChannelLogoutSessionRequired { get; set; }
        public bool AllowOfflineAccess { get; set; }
        public int IdentityTokenLifetime { get; set; }
        public string AllowedIdentityTokenSigningAlgorithms { get; set; }
        public int AccessTokenLifetime { get; set; }
        public int AuthorizationCodeLifetime { get; set; }
        public int? ConsentLifetime { get; set; } = null;
        public int AbsoluteRefreshTokenLifetime { get; set; }
        public int SlidingRefreshTokenLifetime { get; set; }
        public int RefreshTokenUsage { get; set; }
        public bool UpdateAccessTokenClaimsOnRefresh { get; set; }
        public int RefreshTokenExpiration { get; set; }
        public int AccessTokenType { get; set; }
        public bool EnableLocalLogin { get; set; }
        public bool IncludeJwtId { get; set; }
        public bool AlwaysSendClientClaims { get; set; }
        public string ClientClaimsPrefix { get; set; }
        public string PairWiseSubjectSalt { get; set; }
        public int? UserSsoLifetime { get; set; }
        public string UserCodeType { get; set; }
        public int DeviceCodeLifetime { get; set; }

        [Display(Name = "Redirect URIs (one per line)")]
        public string RedirectUris { get; set; }

        [Display(Name = "Allowed Scopes (one per line)")]
        public string AllowedScopes { get; set; }

        [Display(Name = "Allowed Grant Types (one per line)")]
        public string AllowedGrantTypes { get; set; }

        public string PostLogoutRedirectUris { get; set; }
        public string IdentityProviderRestrictions { get; set; }
        public string AllowedCorsOrigins { get; set; }
    }

    /*public async Task<IActionResult> OnGetAsync(int id)
    {
        DbClient = await _dbContext.Clients
            .Include(c => c.RedirectUris)
            .Include(c => c.AllowedScopes)
            .Include(c => c.ClientSecrets)
            .Include(c => c.IdentityProviderRestrictions)
            .Include(c => c.PostLogoutRedirectUris)
            .Include(c => c.AllowedCorsOrigins)
            .Include(c => c.AllowedGrantTypes)
            .SingleOrDefaultAsync(c => c.Id == id);

        if (DbClient == null)
        {
            return NotFound("Client not found");
        }

        Input = new InputModel
        {
            Enabled = DbClient.Enabled,
            ClientId = DbClient.ClientId,
            ClientName = DbClient.ClientName,
            ProtocolType = DbClient.ProtocolType,
            RequireClientSecret = DbClient.RequireClientSecret,
            RequireConsent = DbClient.RequireConsent,
            AllowRememberConsent = DbClient.AllowRememberConsent,
            AlwaysIncludeUserClaimsInIdToken = DbClient.AlwaysIncludeUserClaimsInIdToken,
            RequirePkce = DbClient.RequirePkce,
            AllowPlainTextPkce = DbClient.AllowPlainTextPkce,
            RequireRequestObject = DbClient.RequireRequestObject,
            Description = DbClient.Description,
            ClientUri = DbClient.ClientUri,
            LogoUri = DbClient.LogoUri,
            AllowAccessTokensViaBrowser = DbClient.AllowAccessTokensViaBrowser,
            FrontChannelLogoutUri = DbClient.FrontChannelLogoutUri,
            FrontChannelLogoutSessionRequired = DbClient.FrontChannelLogoutSessionRequired,
            BackChannelLogoutUri = DbClient.BackChannelLogoutUri,
            BackChannelLogoutSessionRequired = DbClient.BackChannelLogoutSessionRequired,
            AllowOfflineAccess = DbClient.AllowOfflineAccess,
            IdentityTokenLifetime = DbClient.IdentityTokenLifetime,
            AllowedIdentityTokenSigningAlgorithms = DbClient.AllowedIdentityTokenSigningAlgorithms,
            AccessTokenLifetime = DbClient.AccessTokenLifetime,
            AuthorizationCodeLifetime = DbClient.AuthorizationCodeLifetime,
            ConsentLifetime = DbClient.ConsentLifetime,
            AbsoluteRefreshTokenLifetime = DbClient.AbsoluteRefreshTokenLifetime,
            SlidingRefreshTokenLifetime = DbClient.SlidingRefreshTokenLifetime,
            RefreshTokenUsage = DbClient.RefreshTokenUsage,
            UpdateAccessTokenClaimsOnRefresh = DbClient.UpdateAccessTokenClaimsOnRefresh,
            RefreshTokenExpiration = DbClient.RefreshTokenExpiration,
            AccessTokenType = DbClient.AccessTokenType,
            EnableLocalLogin = DbClient.EnableLocalLogin,
            IncludeJwtId = DbClient.IncludeJwtId,
            AlwaysSendClientClaims = DbClient.AlwaysSendClientClaims,
            ClientClaimsPrefix = DbClient.ClientClaimsPrefix,
            PairWiseSubjectSalt = DbClient.PairWiseSubjectSalt,
            UserSsoLifetime = DbClient.UserSsoLifetime,
            UserCodeType = DbClient.UserCodeType,
            DeviceCodeLifetime = DbClient.DeviceCodeLifetime,
            RedirectUris = string.Join("\n", DbClient.RedirectUris.Select(c => c.RedirectUri)),
            AllowedGrantTypes = string.Join("\n", DbClient.AllowedGrantTypes.Select(c => c.GrantType)),
            AllowedScopes = string.Join("\n", DbClient.AllowedScopes.Select(c => c.Scope)),
            IdentityProviderRestrictions =
                string.Join("\n", DbClient.IdentityProviderRestrictions.Select(c => c.Provider)),
            PostLogoutRedirectUris =
                string.Join("\n", DbClient.PostLogoutRedirectUris.Select(c => c.PostLogoutRedirectUri)),
            AllowedCorsOrigins = string.Join("\n", DbClient.AllowedCorsOrigins.Select(c => c.Origin)),
        };

        CreateSecretInput = new CreateSecretModel();

        Secrets = DbClient.ClientSecrets;

        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(int id)
    {
        DbClient = await _dbContext.Clients
            .Include(c => c.RedirectUris)
            .Include(c => c.AllowedScopes)
            .Include(c => c.IdentityProviderRestrictions)
            .Include(c => c.PostLogoutRedirectUris)
            .Include(c => c.AllowedCorsOrigins)
            .Include(c => c.AllowedGrantTypes)
            .SingleOrDefaultAsync(c => c.Id == id);

        if (DbClient == null)
        {
            return NotFound("Client not found");
        }

        DbClient.Enabled = Input.Enabled;
        DbClient.ClientId = Input.ClientId;
        DbClient.ClientName = Input.ClientName;
        DbClient.ProtocolType = Input.ProtocolType;
        DbClient.RequireClientSecret = Input.RequireClientSecret;
        DbClient.RequireConsent = Input.RequireConsent;
        DbClient.AllowRememberConsent = Input.AllowRememberConsent;
        DbClient.AlwaysIncludeUserClaimsInIdToken = Input.AlwaysIncludeUserClaimsInIdToken;
        DbClient.RequirePkce = Input.RequirePkce;
        DbClient.AllowPlainTextPkce = Input.AllowPlainTextPkce;
        DbClient.RequireRequestObject = Input.RequireRequestObject;
        DbClient.Description = Input.Description;
        DbClient.ClientUri = Input.ClientUri;
        DbClient.LogoUri = Input.LogoUri;
        DbClient.AllowAccessTokensViaBrowser = Input.AllowAccessTokensViaBrowser;
        DbClient.FrontChannelLogoutUri = Input.FrontChannelLogoutUri;
        DbClient.FrontChannelLogoutSessionRequired = Input.FrontChannelLogoutSessionRequired;
        DbClient.BackChannelLogoutUri = Input.BackChannelLogoutUri;
        DbClient.BackChannelLogoutSessionRequired = Input.BackChannelLogoutSessionRequired;
        DbClient.AllowOfflineAccess = Input.AllowOfflineAccess;
        DbClient.IdentityTokenLifetime = Input.IdentityTokenLifetime;
        DbClient.AllowedIdentityTokenSigningAlgorithms = Input.AllowedIdentityTokenSigningAlgorithms;
        DbClient.AccessTokenLifetime = Input.AccessTokenLifetime;
        DbClient.AuthorizationCodeLifetime = Input.AuthorizationCodeLifetime;
        DbClient.ConsentLifetime = Input.ConsentLifetime;
        DbClient.AbsoluteRefreshTokenLifetime = Input.AbsoluteRefreshTokenLifetime;
        DbClient.SlidingRefreshTokenLifetime = Input.SlidingRefreshTokenLifetime;
        DbClient.RefreshTokenUsage = Input.RefreshTokenUsage;
        DbClient.UpdateAccessTokenClaimsOnRefresh = Input.UpdateAccessTokenClaimsOnRefresh;
        DbClient.RefreshTokenExpiration = Input.RefreshTokenExpiration;
        DbClient.AccessTokenType = Input.AccessTokenType;
        DbClient.EnableLocalLogin = Input.EnableLocalLogin;
        DbClient.IncludeJwtId = Input.IncludeJwtId;
        DbClient.AlwaysSendClientClaims = Input.AlwaysSendClientClaims;
        DbClient.ClientClaimsPrefix = Input.ClientClaimsPrefix;
        DbClient.PairWiseSubjectSalt = Input.PairWiseSubjectSalt;
        DbClient.UserSsoLifetime = Input.UserSsoLifetime;
        DbClient.UserCodeType = Input.UserCodeType;
        DbClient.DeviceCodeLifetime = Input.DeviceCodeLifetime;
        DbClient.RedirectUris = (Input.RedirectUris ?? "")
            .Replace("\r", "")
            .Split("\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(c => new ClientRedirectUri {RedirectUri = c})
            .ToList();
        DbClient.AllowedScopes = (Input.AllowedScopes ?? "")
            .Replace("\r", "")
            .Split("\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(c => new ClientScope {Scope = c})
            .ToList();
        DbClient.AllowedGrantTypes = (Input.AllowedGrantTypes ?? "")
            .Replace("\r", "")
            .Split("\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(c => new ClientGrantType {GrantType = c})
            .ToList();
        DbClient.IdentityProviderRestrictions = (Input.IdentityProviderRestrictions ?? "")
            .Replace("\r", "")
            .Split("\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(c => new ClientIdPRestriction {Provider = c})
            .ToList();
        DbClient.PostLogoutRedirectUris = (Input.PostLogoutRedirectUris ?? "")
            .Replace("\r", "")
            .Split("\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(c => new ClientPostLogoutRedirectUri {PostLogoutRedirectUri = c})
            .ToList();
        DbClient.AllowedCorsOrigins = (Input.AllowedCorsOrigins ?? "")
            .Replace("\r", "")
            .Split("\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(c => new ClientCorsOrigin {Origin = c})
            .ToList();

        await _dbContext.SaveChangesAsync();

        StatusMessage = "Changes saved";

        return RedirectToPage(new {id});
    }

    public async Task<IActionResult> OnPostCreateSecretAsync(int id)
    {
        DbClient = await _dbContext.Clients
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

        return RedirectToPage(new {id});
    }

    public async Task<IActionResult> OnPostDeleteSecretAsync(int secret)
    {
        var dbSecret = await _dbContext.ClientSecrets
            .SingleOrDefaultAsync(c => c.Id == secret);

        if (dbSecret == null)
        {
            return NotFound("Secret not found");
        }

        _dbContext.ClientSecrets.Remove(dbSecret);

        await _dbContext.SaveChangesAsync();

        StatusMessage = "Secret deleted.";

        return RedirectToPage(new {id = dbSecret.ClientId});
    }*/
}
