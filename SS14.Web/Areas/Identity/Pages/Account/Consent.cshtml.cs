#nullable enable
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using SS14.Auth.Shared.Data;
using SS14.Web.Services;

namespace SS14.Web.Areas.Identity.Pages.Account;
// TODO: Replace identityserver4 code in this file
public sealed class Consent : PageModel
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<Consent> _logger;
    private readonly OpenIdActionService _actionService;

    public OpenIddictRequest? AuthRequest { get; private set; }
    public SpaceApplication? Application { get; private set; }
    public string? ReturnUrl { get; set; }

    [BindProperty] public InputModel? Input { get; set; }

    [ValidateAntiForgeryToken]
    public sealed class InputModel
    {
        public string? ReturnUrl { get; set; }
        public string? Button { get; set; }
    }

    public Consent(ApplicationDbContext dbContext, ILogger<Consent> logger, OpenIdActionService actionService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _actionService = actionService;
    }

    public async Task<IActionResult> OnGetAsync(string returnUrl)
    {
        ReturnUrl = returnUrl;
        AuthRequest = HttpContext.GetOpenIddictClientRequest();

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
                return Forbid(validation.Error.Error!, "The login is required.");
        }

        var authorization = await _actionService.AuthorizeActionAsync(AuthRequest, AuthRequest.GetScopes());
        Application = authorization.Application;


        
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
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
