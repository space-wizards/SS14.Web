#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using SS14.Auth.Shared.Data;
using SS14.Web.Helpers;
using Claims = OpenIddict.Abstractions.OpenIddictConstants.Claims;

namespace SS14.Web.Services;

public class IdentityClaimsProvider
{
    private readonly UserManager<SpaceUser> _userManager;

    public IdentityClaimsProvider(UserManager<SpaceUser> userManager)
    {
        _userManager = userManager;
    }

    /// <summary>
    /// Provides claims for the specified identity.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="scopes"></param>
    /// <param name="identity">The identity for which claims are to be provided.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProvideClaimsAsync(string userId, ImmutableArray<string> scopes, ClaimsIdentity identity)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return;

        var username = await _userManager.GetUserNameAsync(user) ?? "";

        identity.AddClaim(new Claim(Claims.Subject, await _userManager.GetUserIdAsync(user)));

        if (scopes.Contains(OpenIddictConstants.Scopes.Profile))
        {
            identity.AddClaim(new Claim(Claims.Name, username));
            identity.AddClaim(new Claim(Claims.PreferredUsername, await _userManager.GetUserNameAsync(user) ?? username));
        }

        if (scopes.Contains(OpenIddictConstants.Scopes.Email))
            identity.AddClaim(new Claim(Claims.Email, await _userManager.GetEmailAsync(user) ?? ""));

        if (scopes.Contains(OpenIddictConstants.Scopes.Roles))
            identity.AddClaims(Claims.Role, ImmutableArray.Create([.. await _userManager.GetRolesAsync(user)]));
    }

    /// <summary>
    /// TODO: Document
    ///
    /// </summary>
    /// <param name="claim"></param>
    /// <returns></returns>
    public IEnumerable<string> GetDestinations(Claim claim)  => claim.Type switch
    {
        Claims.Name or Claims.PreferredUsername => DestinationHelper.Destination(claim, OpenIddictConstants.Scopes.Profile),
        Claims.Email => DestinationHelper.Destination(claim, OpenIddictConstants.Scopes.Email),
        Claims.Role => DestinationHelper.Destination(claim, OpenIddictConstants.Scopes.Roles),
        Claims.Address => DestinationHelper.Destination(claim, OpenIddictConstants.Scopes.Address),
        "AspNet.Identity.SecurityStamp" => DestinationHelper.Destination(),
        _ => DestinationHelper.Destination(OpenIddictConstants.Destinations.AccessToken)
    };
}
