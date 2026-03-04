#nullable enable
using System.Security.Claims;

namespace SS14.Web.OpenId.Types;

public record ConsentResult(ConsentResult.ResultType Type, string? ErrorName, ClaimsPrincipal? Principal)
{
    public static ConsentResult Error(string error) => new(ResultType.Error, error, null);
    public static ConsentResult Forbid(string error) => new(ResultType.Forbid, error, null);
    public static ConsentResult SignIn(ClaimsPrincipal principal) => new(ResultType.SignIn, null, principal);

    public enum ResultType
    {
        Error,
        Forbid,
        SignIn
    }
}
