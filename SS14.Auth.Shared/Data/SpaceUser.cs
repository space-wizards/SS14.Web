using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using SS14.WebEverythingShared;

namespace SS14.Auth.Shared.Data;

public class SpaceUser : IdentityUser<Guid>
{
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// Account has been locked by an administrator and cannot be logged into anymore.
    /// </summary>
    public bool AdminLocked { get; set; }

    /// <summary>
    /// Account requires an email change before being accessible again.
    /// </summary>
    public bool RequireEmailChange { get; set; }

    /// <summary>
    /// Account requires a password change before being accessible again.
    /// </summary>
    public bool RequirePasswordChange { get; set; }

    /// <summary>
    /// Note set by hub administrator.
    /// </summary>
    [Required]
    public string AdminNotes { get; set; } = "";

    /// <summary>
    /// Last time this user changed their user name.
    /// </summary>
    public DateTime? LastUsernameChange { get; set; }

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
    [JsonIgnore]
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
    public long Id { get; set; }
    public Guid SpaceUserId { get; set; }
    [JsonIgnore]
    public SpaceUser SpaceUser { get; set; } = default!;

    public AccountLogType Type { get; set; } = default!;
    [Required] public JsonDocument Data { get; set; } = default!;
    public DateTime Time { get; set; }

    /// <summary>
    /// The user ID of the account that did the action.
    /// </summary>
    /// <remarks>
    /// For most logs this is going to just be the target ID, but for admin actions this is different
    /// (and shouldn't be shown to users).
    /// </remarks>
    public Guid? Actor { get; set; }

    /// <summary>
    /// The IP address that was used to perform the action.
    /// </summary>
    /// <remarks>
    /// Supposed to get purged after 2 weeks.
    /// </remarks>
    [CanBeNull]
    public IPAddress ActorAddress { get; set; }

    public void Dispose() => Data?.Dispose();
}

#nullable enable

public abstract record AccountLogEntry
{
    private static readonly Dictionary<AccountLogType, Type> EnumToType;
    private static readonly Dictionary<Type, AccountLogType> TypeToEnum;
    public static readonly Dictionary<AccountLogType, AccountLogRetainType> EnumToRetention;

    [JsonIgnore]
    public AccountLogType Type => TypeToEnum[GetType()];

    static AccountLogEntry()
    {
        (EnumToType, TypeToEnum) = AuditEntryHelper.CreateEntryMapping<AccountLogType, AccountLogEntry>();

        EnumToRetention = new Dictionary<AccountLogType, AccountLogRetainType>();
        foreach (var enumName in EnumToType.Keys)
        {
            var member = typeof(AccountLogType).GetMember(enumName.ToString())[0];
            var attr = (AccountLogRetentionAttribute?)Attribute.GetCustomAttribute(
                member,
                typeof(AccountLogRetentionAttribute));

            EnumToRetention[enumName] = attr?.Type ?? AccountLogRetainType.Forever;
        }
    }

    public static AccountLogEntry Deserialize(AccountLogType type, JsonDocument document)
    {
        return (AccountLogEntry)(document.Deserialize(EnumToType[type]) ?? throw new InvalidDataException());
    }
}

#nullable restore

public sealed record AccountLogCreated : AccountLogEntry;
public sealed record AccountLogEmailConfirmedChanged(bool NewConfirmed) : AccountLogEntry;

public sealed record AccountLogEmailChanged(string OldEmail, string NewEmail) : AccountLogEntry;
public sealed record AccountLogUserNameChanged(string NewName, string OldName) : AccountLogEntry;
public sealed record AccountLogHubAdminChanged(bool NewAdmin) : AccountLogEntry;
public sealed record AccountLogPasswordChanged : AccountLogEntry;
public sealed record AccountLogPatreonLinked : AccountLogEntry;
public sealed record AccountLogPatreonUnlinked : AccountLogEntry;
public sealed record AccountLogAuthenticatorReset : AccountLogEntry;
public sealed record AccountLogAuthenticatorEnabled : AccountLogEntry;
public sealed record AccountLogAuthenticatorDisabled : AccountLogEntry;
public sealed record AccountLogRecoveryCodesGenerated : AccountLogEntry;

public sealed record AccountLogAdminNotesChanged(string NewNotes) : AccountLogEntry;
public sealed record AccountLogAdminLockedChanged(bool NewLocked) : AccountLogEntry;

public sealed record AccountLogAuthRoleAdded(Guid Role) : AccountLogEntry;
public sealed record AccountLogAuthRoleRemoved(Guid Role) : AccountLogEntry;

public sealed record AccountLogCreatedReserved : AccountLogEntry;

public enum AccountLogType
{
    // @formatter:off
    [AuditEntryType(typeof(AccountLogCreated))]
    [AccountLogRetention(AccountLogRetainType.AccountManagement)]
    Created = 0,

    [AuditEntryType(typeof(AccountLogEmailConfirmedChanged))]
    [AccountLogRetention(AccountLogRetainType.AccountManagement)]
    EmailConfirmedChanged = 1,

    [AuditEntryType(typeof(AccountLogEmailChanged))]
    [AccountLogRetention(AccountLogRetainType.AccountManagement)]
    EmailChanged = 2,

    [AuditEntryType(typeof(AccountLogUserNameChanged))]
    [AccountLogRetention(AccountLogRetainType.AccountManagement)]
    UserNameChanged = 3,

    [AuditEntryType(typeof(AccountLogHubAdminChanged))]
    [AccountLogRetention(AccountLogRetainType.AccountManagement)]
    HubAdminChanged = 4,

    [AuditEntryType(typeof(AccountLogPasswordChanged))]
    [AccountLogRetention(AccountLogRetainType.AccountManagement)]
    PasswordChanged = 5,

    [AuditEntryType(typeof(AccountLogPatreonLinked))]
    [AccountLogRetention(AccountLogRetainType.AccountManagement)]
    PatreonLinked = 6,

    [AuditEntryType(typeof(AccountLogPatreonUnlinked))]
    [AccountLogRetention(AccountLogRetainType.AccountManagement)]
    PatreonUnlinked = 7,

    [AuditEntryType(typeof(AccountLogAuthenticatorReset))]
    [AccountLogRetention(AccountLogRetainType.AccountManagement)]
    AuthenticatorReset = 8,

    [AuditEntryType(typeof(AccountLogAuthenticatorEnabled))]
    [AccountLogRetention(AccountLogRetainType.AccountManagement)]
    AuthenticatorEnabled = 9,

    [AuditEntryType(typeof(AccountLogAuthenticatorDisabled))]
    [AccountLogRetention(AccountLogRetainType.AccountManagement)]
    AuthenticatorDisabled = 10,

    [AuditEntryType(typeof(AccountLogRecoveryCodesGenerated))]
    [AccountLogRetention(AccountLogRetainType.AccountManagement)]
    RecoveryCodesGenerated = 11,

    [AuditEntryType(typeof(AccountLogAdminNotesChanged))]
    [AccountLogRetention(AccountLogRetainType.AccountManagement)]
    AdminNotesChanged = 12,

    [AuditEntryType(typeof(AccountLogAdminLockedChanged))]
    [AccountLogRetention(AccountLogRetainType.AccountManagement)]
    AdminLockedChanged = 13,

    [AuditEntryType(typeof(AccountLogAuthRoleAdded))]
    [AccountLogRetention(AccountLogRetainType.AccountManagement)]
    AuthRoleAdded = 14,

    [AuditEntryType(typeof(AccountLogAuthRoleRemoved))]
    [AccountLogRetention(AccountLogRetainType.AccountManagement)]
    AuthRoleRemoved = 15,

    [AuditEntryType(typeof(AccountLogCreatedReserved))]
    [AccountLogRetention(AccountLogRetainType.AccountManagement)]
    CreatedReserved = 16,
    // @formatter:on
}

public enum AccountLogRetainType
{
    /// <summary>
    /// Logs are retained FOREVER.
    /// </summary>
    Forever = 0,

    /// <summary>
    /// Detailed logs, such as server joining.
    /// </summary>
    Detail,

    /// <summary>
    /// Logs about account management actions such as password changes.
    /// </summary>
    AccountManagement
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class AccountLogRetentionAttribute(AccountLogRetainType type) : Attribute
{
    public AccountLogRetainType Type { get; } = type;
}
