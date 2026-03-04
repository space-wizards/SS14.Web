using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SS14.Web.Helpers;

public static class ScopeHelper
{
    // ReSharper disable once ArrangeMethodOrOperatorBody
    public static string GetScopeName(string scope) => scope switch
    {
        Scopes.Profile => "Name and user ID",
        Scopes.Email => "Email",
        Scopes.Roles => "Roles",
        _ => scope,
    };
}
