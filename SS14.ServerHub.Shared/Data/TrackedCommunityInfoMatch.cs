using System.ComponentModel.DataAnnotations.Schema;

namespace SS14.ServerHub.Shared.Data;

/// <summary>
/// Represents a server match for a <see cref="TrackedCommunity"/> based on matching JSON info.
/// </summary>
public sealed class TrackedCommunityInfoMatch
{
    /// <summary>
    /// The ID of this entity in the database.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// JSON path to match. This is compared with the PostgreSQL <c>jsonb_path_exists</c> function.
    /// </summary>
    [Column(TypeName = "jsonpath")]
    public string Path { get; set; } = default!;

    /// <summary>
    /// Which data in the server information should be matched match.
    /// </summary>
    public InfoMatchField Field { get; set; }

    /// <summary>
    /// The ID of the <see cref="TrackedCommunity"/> this match belongs to.
    /// </summary>
    public int TrackedCommunityId { get; set; }

    // Navigation properties
    public TrackedCommunity TrackedCommunity { get; set; } = default!;
}

/// <summary>
/// Which field of server data to test against with a <see cref="TrackedCommunityInfoMatch"/>.
/// </summary>
public enum InfoMatchField
{
    /// <summary>
    /// Matches the status field of the server response.
    /// </summary>
    Status = 0,

    /// <summary>
    /// Matches the info field of the server response.
    /// </summary>
    Info = 1
}