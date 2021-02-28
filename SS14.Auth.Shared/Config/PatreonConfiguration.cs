using System.Collections.Generic;

namespace SS14.Auth.Shared.Config
{
    public sealed class PatreonConfiguration
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public Dictionary<string, string> TierMap { get; set; }
        public Dictionary<string, string> TierNames { get; set; }
        public string WebhookSecret { get; set; }
        public bool LogWebhooks { get; set; } = true;
    }
}