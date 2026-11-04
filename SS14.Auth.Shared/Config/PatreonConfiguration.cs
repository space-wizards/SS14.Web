namespace SS14.Auth.Shared.Config;

public sealed class PatreonConfiguration
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public bool LogWebhooks { get; set; } = true;

    /// <summary>
    /// Public base URL of the auth API, used to tell Patreon where to send webhook messages
    /// </summary>
    public string WebhookBaseUrl { get; set; }
}
