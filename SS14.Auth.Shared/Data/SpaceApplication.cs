using System;
using OpenIddict.EntityFrameworkCore.Models;
using static SS14.Auth.Shared.Data.OpeniddictDefaultTypes;

namespace SS14.Auth.Shared.Data;

public sealed class SpaceApplication : OpenIddictEntityFrameworkCoreApplication<string, DefaultAuthorization, DefaultToken>
{
    public Guid SpaceUserId { get; set; }
    public SpaceUser SpaceUser { get; set; }
}
