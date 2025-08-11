#nullable enable
using System.Collections.Generic;
using System.Security.Claims;
using OpenIddict.Abstractions;

namespace SS14.Web.Helpers;

public static class DestinationHelper
{
    /// <summary>
    /// Yield the access token destination and additionally the identity token destination
    /// if the given scope is present in the claim
    /// </summary>
    /// <param name="claim">The claim to return destinations for</param>
    /// <param name="scope">The scope to use for checking if identity destination should be returned</param>
    /// <returns>Destinations.AccessToken and optionally the Destinations.IdentityToken</returns>
    public static IEnumerable<string> Destination(Claim claim, string scope)
    {
        yield return OpenIddictConstants.Destinations.AccessToken;

        if (claim.Subject!.HasScope(scope))
            yield return OpenIddictConstants.Destinations.IdentityToken;
    }

    /// <summary>
    /// Yields the given destination
    /// </summary>
    /// <param name="destination">The destination to yield</param>
    /// <returns>The given destination</returns>
    /// <remarks>This is useful in a switch for determining the destinations of a claim</remarks>
    public static IEnumerable<string> Destination(string destination)
    {
        yield return destination;
    }

    /// <summary>
    /// Completely skips a claim by yielding nothing
    /// </summary>
    /// <returns>Nothing</returns>
    /// <remarks>This is useful in a switch for determining the destinations of a claim</remarks>
    public static IEnumerable<string> Destination()
    {
        yield break;
    }
}
