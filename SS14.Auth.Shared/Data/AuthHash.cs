using System;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace SS14.Auth.Shared.Data;

public class AuthHash
{
    public long AuthHashId { get; set; }

    public Guid SpaceUserId { get; set; }
    public SpaceUser SpaceUser { get; set; }

    /// <summary>
    /// The HWID for the connection attempt from the client.
    /// </summary>
    public long? HwidId { get; set; }
    [CanBeNull] public Hwid Hwid { get; set; }

    public DateTimeOffset Expires { get; set; }

    [Required]
    public byte[] Hash { get; set; }
}
