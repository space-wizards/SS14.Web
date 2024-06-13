namespace SS14.Auth.Shared.Config;

/// <summary>
/// Contains configuration for how long aspects of account logs are retained.
/// </summary>
public sealed class AccountLogRetentionConfiguration
{
    /// <summary>
    /// How long IP addresses in the logs are retained.
    /// </summary>
    public int IPRetainDays { get; set; } = 14;

    /// <summary>
    /// Logs about detailed actions, such as every time somebody connects to a game server.
    /// </summary>
    public int DetailRetainDays { get; set; } = 14;

    /// <summary>
    /// Logs about account management things, such as changing password.
    /// </summary>
    // "as good as forever"
    public int AccountManagementRetainDays { get; set; } = 365 * 200;
}