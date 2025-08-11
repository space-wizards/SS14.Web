#nullable enable
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Services;

public class SignedInIdentityService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<SpaceUser> _userManager;
    private readonly SignInManager<SpaceUser> _signInManager;

    public SignedInIdentityService(IHttpContextAccessor httpContextAccessor, UserManager<SpaceUser> userManager, SignInManager<SpaceUser> signInManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<bool> IsAvailableAsync()
    {
        if (_httpContextAccessor.HttpContext is not { } context)
            return false;

        var result = await context.AuthenticateAsync();
        return result.Succeeded;
    }

    public async Task<string?> GetUserIdAsync()
    {
        if (_httpContextAccessor.HttpContext is not { } context)
            return null;

        var user = await _userManager.GetUserAsync(context.User);
        if (user is null)
            return null;

        return await _userManager.GetUserIdAsync(user);
    }
}
