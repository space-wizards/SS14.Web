#nullable enable
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using SS14.Auth.Shared.Data;
using SS14.Web.Helpers;
using SS14.Web.Models.Types;
using SS14.Web.Services;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SS14.Web.Areas.Identity.Pages.Account;
// TODO: Replace identityserver4 code in this file
public sealed class Consent : PageModel
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<Consent> _logger;
    private readonly OpenIdActionService _actionService;

    public OpenIddictRequest? AuthRequest { get; private set; }
    public SpaceApplication? Application { get; private set; }
    public ImmutableArray<string> RequestScopes { get; private set; }
    public string? ReturnUrl { get; set; }

    [BindProperty] public InputModel? Input { get; set; }

    [ValidateAntiForgeryToken]
    public sealed class InputModel
    {
        public string? Button { get; set; }
    }

    public Consent(ApplicationDbContext dbContext, ILogger<Consent> logger, OpenIdActionService actionService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _actionService = actionService;
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl)
    {
        return await HandleAuthorization(returnUrl);
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl)
    {
        switch (Input?.Button)
        {
            case "no":
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            case "yes":
                return await HandleAccept();
            default:
                return await HandleAuthorization(returnUrl);

                return NotFound();
                /*var request = await _interaction.GetAuthorizationContextAsync(Input.ReturnUrl);
        if (request == null)
            return RedirectToAction("Error", "Home");

        var response = new ConsentResponse();
        if (Input.Button == "yes")
        {
            var resources = request.ValidatedResources.Resources;

            // TODO: Maybe allow configuring this?
            response.RememberConsent = true;
            response.ScopesValuesConsented = resources.ApiScopes.Select(x => x.Name)
                .Concat(resources.IdentityResources.Select(x => x.Name));
        }
        else if (Input.Button == "no")
        {
            response.Error = AuthorizationError.AccessDenied;
        }
        else
        {
            return BadRequest();
        }

        await _interaction.GrantConsentAsync(request, response);

        return Redirect(Input.ReturnUrl);*/
        }
    }

    public static string GetScopeName(string scope) => scope switch
    {
        Scopes.Email => "Email",
        Scopes.Roles => "Roles",
        _ => scope,
    };

    private async Task<IActionResult> HandleAuthorization(string? returnUrl)
    {
        ReturnUrl = returnUrl;
        AuthRequest = HttpContext.GetOpenIddictServerRequest();

        if (AuthRequest is null)
            return BadRequest();

        var result = await HttpContext.AuthenticateAsync();
        var validation = _actionService.ValidateOpenIdAuthentication(
            HttpContext,
            result,
            AuthRequest);

        switch (validation.IsSuccess)
        {
            case false when validation.Error.IsChallenge:
                return Challenge(validation.Error.Properties!);
            case false:
                return AuthResults.Forbid(validation.Error.Error!, GetErrorDescription(validation.Error.Error!));
        }

        // Ensure the request contains the openid and profile scopes
        RequestScopes = [..AuthRequest.GetScopes().Union(["openid", "profile"])];
        var authorization = await _actionService.AuthorizeActionAsync(AuthRequest, RequestScopes);
        Application = authorization.Application;

        return authorization.Type switch
        {
            AuthorizationResult.ResultType.Forbidden =>
                AuthResults.Forbid(authorization.ErrorName ?? "", GetErrorDescription(authorization.ErrorName)),

            AuthorizationResult.ResultType.Error =>
                BadRequest(GetErrorDescription(authorization.ErrorName)),

            AuthorizationResult.ResultType.SignIn =>
                SignIn(authorization.Principal!, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme),

            _ => Page(),
        };
    }

    private async Task<IActionResult> HandleAccept()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request is null)
            return BadRequest("The OpenID Connect request cannot be retrieved.");

        var result = await _actionService.AcceptActionAsync(request, request.GetScopes());

        return result.Type switch
        {
            ConsentResult.ResultType.SignIn =>
                SignIn(result.Principal!,
                    authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme),
            ConsentResult.ResultType.Forbid =>
                 AuthResults.Forbid(result.ErrorName ?? "", GetErrorDescription(result.ErrorName)),
            _ => BadRequest(GetErrorDescription(result.ErrorName))
        };
    }

    // ReSharper disable once ArrangeMethodOrOperatorBody
    private static string GetErrorDescription(string? error) => error switch
    {
        OpenIdActionService.ApplicationNotFoundError => "No application for the given client id.",
        Errors.AccessDenied => "The logged in user is not allowed to access this client application.",
        Errors.ConsentRequired => "Interactive user consent is required.",
        Errors.UnsupportedGrantType => "The specified grant type is not supported.",
        Errors.InvalidGrant => "The token is no longer valid.",
        _ => "An unknown error occurred.",
    };
}
