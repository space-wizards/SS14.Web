using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SS14.Auth.Shared.Data;

/// <summary>
/// A single HWID known to the authorization database.
/// </summary>
public sealed class Hwid
{
    /// <summary>
    /// Type code for <see cref="TypeCode"/>.
    /// </summary>
    public const int Type1 = 1;

    public long Id { get; set; }

    /// <summary>
    /// Intended to be used for administration of the database.
    /// </summary>
    public int TypeCode { get; set; }

    /// <summary>
    /// Data returned by client.
    /// </summary>
    [Required] public byte[] ClientData { get; set; }

    /// <summary>
    /// ID value given to servers.
    /// </summary>
    [Required] public byte[] Value { get; set; }

    /// <summary>
    /// Users that have been "spotted" using this HWID.
    /// </summary>
    [JsonIgnore]
    public List<HwidUser> Users { get; set; } = default!;
}

/// <summary>
/// A hit of a user ever having used an HWID.
/// </summary>
public sealed class HwidUser
{
    public long Id { get; set; }

    /// <summary>
    /// ID of the <see cref="Hwid"/> that this user was seen to be using.
    /// </summary>
    public long HwidId { get; set; }
    public Guid SpaceUserId { get; set; }

    /// <summary>
    /// When the user was first seen using this HWID.
    /// </summary>
    public DateTime FirstSeen { get; set; }

    public Hwid Hwid { get; set; } = null!;

    [JsonIgnore]
    public SpaceUser SpaceUser { get; set; } = null!;
}
