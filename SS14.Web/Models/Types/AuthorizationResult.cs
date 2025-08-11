#nullable enable
using System.Collections.Immutable;
using System.Security.Claims;
using OpenIddict.EntityFrameworkCore.Models;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Models.Types;

public record AuthorizationResult(
    AuthorizationResult.ResultType Type,
    SpaceApplication? Application,
    string? ErrorName,
    ImmutableArray<string>? Scopes,
    ClaimsPrincipal? Principal)
{
    public static AuthorizationResult SignIn(ClaimsPrincipal principal) =>
        new(ResultType.SignIn, null, null, null, principal);

    public static AuthorizationResult Forbidden(SpaceApplication app, string error)
    {
        return new AuthorizationResult(ResultType.Forbidden, app, error, null, null);
    }

    public static AuthorizationResult Consent(SpaceApplication app, ImmutableArray<string> scopes)
    {
        return new AuthorizationResult(ResultType.Consent, app, null, scopes, null);
    }

    public static AuthorizationResult Error(SpaceApplication? app, string error)
    {
        return new AuthorizationResult(ResultType.Error, app, error, null, null);
    }

    public enum ResultType
    {
        SignIn,
        Forbidden,
        Consent,
        Error,
    }
}
