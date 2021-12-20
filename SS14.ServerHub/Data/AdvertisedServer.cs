using System;

namespace SS14.ServerHub.Data
{
    public sealed class AdvertisedServer
    {
        public int AdvertisedServerId { get; set; }
        
        /// <summary>
        /// The address of the server. Must be ss14:// or ss14s:// URI.
        /// </summary>
        public string Address { get; set; } = default!;
        
        /// <summary>
        /// Secret the server gave us when advertising itself.
        /// This secret can be used to delete the advertisement later.
        /// This is actually the SHA256 hash of the secret, not the actual secret given.
        /// </summary>
        public byte[] Secret { get; set; } = default!;
        
        /// <summary>
        /// The time at which this advertisement will stop showing up.
        /// </summary>
        public DateTime Expires { get; set; } = default!;
    }
}