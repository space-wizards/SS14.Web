namespace SS14.Auth.Shared;

public static class AuthConstants
{
    /// <summary>
    /// User has any admin role.
    /// </summary>
    public const string PolicyAnyHubAdmin = "AnyHubAdmin";

    /// <summary>
    /// User has the ability to mess with the accounts list and OAuth clients.
    /// </summary>
    public const string PolicySysAdmin = "SysAdmin";
    public const string RoleSysAdmin = "SysAdmin";
    
    /// <summary>
    /// User has the ability to mess with the game server hub.
    /// </summary>
    public const string PolicyServerHubAdmin = "ServerHubAdmin";
    public const string RoleServerHubAdmin = "ServerHubAdmin";
}