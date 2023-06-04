using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;

namespace SS14.Auth.Shared.Data;

public class SpaceUser : IdentityUser<Guid>
{
    public DateTimeOffset CreatedTime { get; set; }
    
    /// <summary>
    /// Account has been locked by an administrator and cannot be logged into anymore.
    /// </summary>
    public bool AdminLocked { get; set; }
    
    /// <summary>
    /// Note set by hub administrator.
    /// </summary>
    [Required]
    public string AdminNotes { get; set; } = "";
    
    public List<LoginSession> LoginSessions { get; set; } = new List<LoginSession>();
    public List<AuthHash> AuthHashes { get; set; } = new List<AuthHash>();
    
    public Patron Patron { get; set; }

    public List<PastAccountName> PastAccountNames { get; set; } = default!;
    public List<AccountLog> AccountLogs { get; set; } = default!;
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

/// <summary>
/// Log for actions like edits taken on accounts.
/// </summary>
public sealed class AccountLog : IDisposable
{
    public int Id { get; set; }
    public Guid SpaceUserId { get; set; }
    public SpaceUser SpaceUser { get; set; } = default!;

    public AccountLogType Type { get; set; } = default!;
    [Required] public JsonDocument Data { get; set; } = default!;
    public DateTime Time { get; set; }

    public void Dispose() => Data?.Dispose();

    public AccountLogEntry DataEntry
    {
        get
        {
            return Type switch
            {
                AccountLogType.Created => Data.Deserialize<AccountLogCreated>(),
                AccountLogType.EmailConfirmedChanged => Data.Deserialize<AccountLogEmailConfirmedChanged>(),
                AccountLogType.EmailChanged => Data.Deserialize<AccountLogEmailChanged>(),
                AccountLogType.UserNameChanged => Data.Deserialize<AccountLogUserNameChanged>(),
                AccountLogType.HubAdminChanged => Data.Deserialize<AccountLogHubAdminChanged>(),
                AccountLogType.PasswordChanged => Data.Deserialize<AccountLogPasswordChanged>(),
                AccountLogType.PatreonLinked => Data.Deserialize<AccountLogPatreonLinked>(),
                AccountLogType.PatreonUnlinked => Data.Deserialize<AccountLogPatreonUnlinked>(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}

public abstract record AccountLogEntry;

public sealed record AccountLogCreated : AccountLogEntry;
public sealed record AccountLogEmailConfirmedChanged(bool NewConfirmed, Guid Actor) : AccountLogEntry;

public sealed record AccountLogEmailChanged(string OldEmail, string NewEmail, Guid Actor) : AccountLogEntry;
public sealed record AccountLogUserNameChanged(string NewName, string OldName, Guid Actor) : AccountLogEntry;
public sealed record AccountLogHubAdminChanged(bool NewAdmin, Guid Actor) : AccountLogEntry;
public sealed record AccountLogPasswordChanged(Guid Actor) : AccountLogEntry;
public sealed record AccountLogPatreonLinked(Guid Actor) : AccountLogEntry;
public sealed record AccountLogPatreonUnlinked(Guid Actor) : AccountLogEntry;
public sealed record AccountLogAuthenticatorReset(Guid Actor) : AccountLogEntry;
public sealed record AccountLogAuthenticatorEnabled(Guid Actor) : AccountLogEntry;
public sealed record AccountLogAuthenticatorDisabled(Guid Actor) : AccountLogEntry;
public sealed record AccountLogRecoveryCodesGenerated(Guid Actor) : AccountLogEntry;

public sealed record AccountLogAdminNotesChanged(string NewNotes, Guid Actor) : AccountLogEntry;
public sealed record AccountLogAdminLockedChanged(bool NewLocked, Guid Actor) : AccountLogEntry;

public enum AccountLogType
{
    Created = 0,
    EmailConfirmedChanged = 1,
    EmailChanged = 2,
    UserNameChanged = 3,
    HubAdminChanged = 4,
    PasswordChanged = 5,
    PatreonLinked = 6,
    PatreonUnlinked = 7,
    AuthenticatorReset = 8,
    AuthenticatorEnabled = 9,
    AuthenticatorDisabled = 10,
    RecoveryCodesGenerated = 11,
    AdminNotesChanged = 12,
    AdminLockedChanged = 13,
}