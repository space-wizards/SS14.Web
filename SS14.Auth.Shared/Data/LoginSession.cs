using System;
using System.ComponentModel.DataAnnotations;

namespace SS14.Auth.Shared.Data;

public class LoginSession
{
    public long LoginSessionId { get; set; }

    public Guid SpaceUserId { get; set; }
    public SpaceUser SpaceUser { get; set; }

    public DateTimeOffset Expires { get; set; }

    [Required]
    public byte[] Token { get; set; }
}