#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using SS14.Auth.Shared.Data;
using SS14.Web.Extensions;
using SS14.Web.Models.Types;
using SS14.Web.OpenId.Types;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Void = SS14.Web.Models.Types.Void;

namespace SS14.Web.OpenId.Services;

public class OpenIdActionService
{
    public const string ApplicationNotFoundError = "application_not_found";
    private const string IgnoreChallengeKey = "IgnoreAuthenticationChallenge";

    private readonly SignedInIdentityService _signedInIdentity;
    private readonly IdentityClaimsProvider _claimsProvider;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly OpenIddictApplicationManager<SpaceApplication> _applicationManager;
    private readonly ILogger<OpenIdActionService> _logger;

    public OpenIdActionService(SignedInIdentityService signedInIdentity,
        IdentityClaimsProvider claimsProvider,
        OpenIddictApplicationManager<SpaceApplication> applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        ILogger<OpenIdActionService> logger,
        IOpenIddictScopeManager scopeManager)
    {
        _signedInIdentity = signedInIdentity;
        _claimsProvider = claimsProvider;
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _logger = logger;
        _scopeManager = scopeManager;
    }

    public Result<Void, AuthenticationValidationFailure> ValidateOpenIdAuthentication(
        HttpContext context,
        bool ignoreChallenge,
        AuthenticateResult auth,
        OpenIddictRequest request)
    {
        // Auth succeeded and nothing is forcing re-authentication
        if (auth.Succeeded
            && !request.HasPromptValue(PromptValues.Login)
            && request.MaxAge is not 0
            && (request.MaxAge is null || auth.Properties?.IssuedUtc is null || TimeProvider.System.GetUtcNow() - auth.Properties.IssuedUtc < TimeSpan.FromSeconds(request.MaxAge.Value)))
        {
            return Result<Void, AuthenticationValidationFailure>.Success(Void.Nothing);
        }

        if (ignoreChallenge || request.HasPromptValue(PromptValues.None))
            return Result<Void, AuthenticationValidationFailure>.Failure(AuthenticationValidationFailure.LoginRequired);

        var properties = new AuthenticationProperties
        {
            RedirectUri = context.Request.PathBase + context.Request.Path + QueryString.Create(
                context.Request.HasFormContentType ? context.Request.Form : context.Request.Query),
        };

        return Result<Void, AuthenticationValidationFailure>.Failure(AuthenticationValidationFailure.Challenge(properties));
    }

    public async Task<AuthorizationResult> AuthorizeActionAsync(OpenIddictRequest request, ImmutableArray<string> scopes)
    {
        var application = await _applicationManager.FindByClientIdAsync(request.ClientId!);
        if (application is null)
            return AuthorizationResult.Error(null,  ApplicationNotFoundError);

        if (!await _signedInIdentity.IsAvailableAsync())
        {
            _logger.LogWarning("Signed in user is not available.");
            return AuthorizationResult.Forbidden(application, Errors.AccessDenied);
        }

        var authorizations = new List<object>();

        // Ensure the user will be prompted if a prompt was requested later in the switch statement below
        if (!request.HasPromptValue(PromptValues.Consent))
        {
            authorizations = await _authorizationManager.FindAsync(
                subject: await _signedInIdentity.GetUserIdAsync(),
                client: await _applicationManager.GetIdAsync(application),
                status: Statuses.Valid,
                type: AuthorizationTypes.Permanent,
                scopes: scopes
            ).ToListAsync();
        }

        return await _applicationManager.GetConsentTypeAsync(application) switch
        {
            ConsentTypes.External when authorizations.Count is 0 =>
               AuthorizationResult.Forbidden(application,  Errors.AccessDenied),

            ConsentTypes.Implicit or ConsentTypes.External or ConsentTypes.Explicit when authorizations.Count is not 0 =>
                await HandleSignIn(request, application, authorizations),

            ConsentTypes.Explicit or ConsentTypes.Systematic when request.HasPromptValue(PromptValues.None) =>
                AuthorizationResult.Forbidden(application,  Errors.ConsentRequired),

            _ => AuthorizationResult.Consent(application, scopes),
        };
    }

    public async Task<ConsentResult> AcceptActionAsync(OpenIddictRequest request, ImmutableArray<string> scopes)
    {
        var application = await _applicationManager.FindByClientIdAsync(request.ClientId!);
        if (application is null)
            return ConsentResult.Error(ApplicationNotFoundError);

        if (!await _signedInIdentity.IsAvailableAsync())
        {
            _logger.LogWarning("Signed in user is not available.");
            return ConsentResult.Forbid(Errors.AccessDenied);
        }

        var userId = await _signedInIdentity.GetUserIdAsync();
        var authorizations = await _authorizationManager.FindAsync(
            subject: userId,
            client: await _applicationManager.GetIdAsync(application),
            status: Statuses.Valid,
            type: AuthorizationTypes.Permanent,
            scopes: scopes
        ).ToListAsync();

        if (authorizations.Count is 0 && await _applicationManager.HasConsentTypeAsync(application, ConsentTypes.External))
            return ConsentResult.Forbid(Errors.AccessDenied);

        var principal = await CreateAuthorizedPrincipal(
            userId!,
            application,
            authorizations,
            scopes,
            _claimsProvider.GetDestinations);

        return ConsentResult.SignIn(principal);
    }

    private async Task<AuthorizationResult> HandleSignIn(
        OpenIddictRequest request,
        SpaceApplication application,
        List<object> authorizations)
    {
        var scopes = request.GetScopes();
        var userId = await _signedInIdentity.GetUserIdAsync();
        var identity = await CreateAuthorizedPrincipal(userId!, application, authorizations, scopes, _claimsProvider.GetDestinations);

        return AuthorizationResult.SignIn(identity);
    }

    private async Task<ClaimsPrincipal> CreateAuthorizedPrincipal(
        string userId,
        SpaceApplication application,
        List<object> authorizations,
        ImmutableArray<string> scopes,
        Func<Claim, IEnumerable<string>> destinationsSelector)
    {
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        await _claimsProvider.ProvideClaimsAsync(userId, scopes, identity);

        identity.SetScopes(scopes);
        identity.SetResources(await _scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

        var authorization = authorizations.LastOrDefault();
        authorization ??= await _authorizationManager.CreateAsync(
            identity: identity,
            subject: userId,
            client: (await _applicationManager.GetIdAsync(application))!,
            type: AuthorizationTypes.Permanent,
            scopes: identity.GetScopes()
        );

        identity.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorization));
        identity.SetDestinations(destinationsSelector);
        return new ClaimsPrincipal(identity);
    }
}
