using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SS14.Auth.Shared.Data;

public class SpaceUser : IdentityUser<Guid>
{
    public DateTimeOffset CreatedTime { get; set; }
    public List<LoginSession> LoginSessions { get; set; } = new List<LoginSession>();
    public List<AuthHash> AuthHashes { get; set; } = new List<AuthHash>();

    public Patron Patron { get; set; }

    public List<PastAccountName> PastAccountNames { get; set; } = default!;
}

public sealed class PastAccountName
{
    public int Id { get; set; }

    public Guid SpaceUserId { get; set; }
    public SpaceUser SpaceUser { get; set; } = default!;

    [Required]
    public string PastName { get; set; }

    [Column(TypeName = "timestamp with time zone")]
    public DateTime ChangeTime { get; set; }
}