using OpenIddict.EntityFrameworkCore.Models;

namespace SS14.Auth.Shared.Data;

public class OpeniddictDefaultTypes
{
    public sealed class DefaultAuthorization : OpenIddictEntityFrameworkCoreAuthorization<string, SpaceApplication, DefaultToken>;
    public sealed class DefaultScope : OpenIddictEntityFrameworkCoreScope<string>;
    public sealed class DefaultToken : OpenIddictEntityFrameworkCoreToken<string, SpaceApplication, DefaultAuthorization>;

}
