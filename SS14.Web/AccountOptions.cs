namespace SS14.Web;

public sealed class AccountOptions
{
    /// <summary>
    /// Delay between allowing people to change their username.
    /// </summary>
    public int UsernameChangeDays { get; set; } = 30;
}