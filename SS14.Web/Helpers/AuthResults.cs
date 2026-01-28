using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Server.AspNetCore.OpenIddictServerAspNetCoreConstants;

namespace SS14.Web.Helpers;

public static class AuthResults
{
    public static ForbidResult Forbid(string error, string description)
    {
        return new ForbidResult(
            authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [Properties.Error] = error,
                [Properties.ErrorDescription] = description,
            }));
    }
}
