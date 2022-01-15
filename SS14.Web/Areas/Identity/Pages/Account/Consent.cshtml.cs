using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Areas.Identity.Pages.Account;

public sealed class Consent : PageModel
{
    private readonly IIdentityServerInteractionService _interaction;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<Consent> _logger;

    public AuthorizationRequest AuthRequest { get; private set; }
    public UserOAuthClient UserClient { get; private set; }
    public string ReturnUrl { get; set; }
    
    [BindProperty] public InputModel Input { get; set; }

    [ValidateAntiForgeryToken]
    public sealed class InputModel
    {
        public string ReturnUrl { get; set; }
        public string Button { get; set; }
    }
    
    public Consent(IIdentityServerInteractionService interaction, ApplicationDbContext dbContext, ILogger<Consent> logger)
    {
        _interaction = interaction;
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<IActionResult> OnGetAsync(string returnUrl)
    {
        ReturnUrl = returnUrl;
        AuthRequest = await _interaction.GetAuthorizationContextAsync(returnUrl);

        // Can be null if managed by hub admins.
        // Though I doubt we'll ever have consent pages for those.
        UserClient = await _dbContext.UserOAuthClients
            .Include(oauth => oauth.Client)
            .Include(oauth => oauth.SpaceUser)
            .SingleOrDefaultAsync(oauth => oauth.Client.ClientId == AuthRequest.Client.ClientId);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var request = await _interaction.GetAuthorizationContextAsync(Input.ReturnUrl);
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

        return Redirect(Input.ReturnUrl);
    }
}