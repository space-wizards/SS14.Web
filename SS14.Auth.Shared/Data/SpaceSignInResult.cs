using Microsoft.AspNetCore.Identity;

namespace SS14.Auth.Shared.Data;

public sealed class SpaceSignInResult : SignInResult
{
    public bool IsAdminLocked { get; private set; }
    public bool EmailChangeRequired { get; private set; }
    public bool PasswordChangeRequired { get; private set; }

    public static readonly SpaceSignInResult AdminLocked = new() { IsAdminLocked = true };
    public static readonly SpaceSignInResult RequireEmailChange = new() { EmailChangeRequired = true };
    public static readonly SpaceSignInResult RequirePasswordChange = new() { PasswordChangeRequired = true };
}
