namespace SS14.Auth.Shared.Config;

public sealed class AccountConfiguration
{
    /// <summary>
    /// Delete unconfirmed accounts after this many days.
    /// </summary>
    public int DeleteUnconfirmedAfter { get; set; } = 3;
}