using Microsoft.AspNetCore.Identity;

namespace SS14.Auth.Shared.Data;

public sealed class SpaceSignInResult : SignInResult
{
    public bool IsAdminLocked { get; private set; }

    public static readonly SpaceSignInResult AdminLocked = new() { IsAdminLocked = true };
}