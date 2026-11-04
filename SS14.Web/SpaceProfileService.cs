using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.AspNetIdentity;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Identity;
using SS14.Auth.Shared.Data;

namespace SS14.Web;

/// <summary>
/// Adds account security information to the claims released to OAuth clients.
/// Can be expanded in the future to include stuff like passkeys or steam logins, should we want.
/// </summary>
public sealed class SpaceProfileService : ProfileService<SpaceUser>
{
    public const string SecurityScope = "security";
    public const string TwoFactorEnabledClaim = "two_factor_enabled";

    public SpaceProfileService(UserManager<SpaceUser> userManager, IUserClaimsPrincipalFactory<SpaceUser> claimsFactory)
        : base(userManager, claimsFactory)
    {
    }

    protected override async Task GetProfileDataAsync(ProfileDataRequestContext context, SpaceUser user)
    {
        await base.GetProfileDataAsync(context, user);

        if (context.RequestedClaimTypes.Contains(TwoFactorEnabledClaim))
        {
            context.IssuedClaims.Add(new Claim(
                TwoFactorEnabledClaim,
                user.TwoFactorEnabled ? "true" : "false",
                ClaimValueTypes.Boolean));
        }
    }
}
