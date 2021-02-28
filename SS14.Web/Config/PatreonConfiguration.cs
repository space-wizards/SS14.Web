using System.Collections.Generic;

namespace SS14.Web.Config
{
    public sealed class PatreonConfiguration
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public Dictionary<string, string> TierMap { get; set; }
        public Dictionary<string, string> TierNames { get; set; }
    }
}