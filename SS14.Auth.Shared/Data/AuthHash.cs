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

    /// <summary>
    /// If true, the client granted the server it is connecting to permission to see security info about the user,
    /// such as whether 2FA is enabled. Only released through hasJoined for this single connection attempt.
    /// </summary>
    public bool RequestSecurity { get; set; }

    public DateTimeOffset Expires { get; set; }

    [Required]
    public byte[] Hash { get; set; }
}
