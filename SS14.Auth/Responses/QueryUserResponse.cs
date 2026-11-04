using System;
using System.Text.Json.Serialization;

namespace SS14.Auth.Responses;

public sealed record QueryUserResponse(
    string UserName,
    Guid UserId,
    string? PatronTier,
    DateTimeOffset CreatedTime,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    CommunityPatronResponse? CommunityPatron = null)
{
}

public sealed record CommunityPatronResponse(string? PatronStatus, string? Tier);
