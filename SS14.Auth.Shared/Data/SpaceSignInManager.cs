using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SS14.Auth.Shared.Data;

[UsedImplicitly]
public sealed class SpaceSignInManager : SignInManager<SpaceUser>
{
    public SpaceSignInManager(
        UserManager<SpaceUser> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<SpaceUser> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<SpaceUser>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<SpaceUser> confirmation
    ) : base(
        userManager,
        contextAccessor,
        claimsFactory,
        optionsAccessor,
        logger,
        schemes,
        confirmation)
    {
    }

    protected override async Task<SignInResult> PreSignInCheck(SpaceUser user)
    {
        if (user.AdminLocked)
            return SpaceSignInResult.AdminLocked;

        return await base.PreSignInCheck(user);
    }
}