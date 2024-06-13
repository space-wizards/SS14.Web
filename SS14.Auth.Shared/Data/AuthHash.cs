using System;
using System.ComponentModel.DataAnnotations;

namespace SS14.Auth.Shared.Data;

public class AuthHash
{
    public long AuthHashId { get; set; }

    public Guid SpaceUserId { get; set; }
    public SpaceUser SpaceUser { get; set; }

    public DateTimeOffset Expires { get; set; }

    [Required]
    public byte[] Hash { get; set; }
}