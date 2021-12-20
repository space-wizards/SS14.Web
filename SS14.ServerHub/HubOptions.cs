namespace SS14.ServerHub
{
    public sealed class HubOptions
    {
        public const string Position = "Hub";
        
        public float AdvertisementExpireMinutes { get; set; } = 3;
        
        /// <summary>
        /// When a server advertises itself with the hub, we check whether we can reach the address.
        /// This is the timeout for that test.
        /// </summary>
        public float AdvertisementStatusTestTimeoutSeconds { get; set; } = 5;
    }
}