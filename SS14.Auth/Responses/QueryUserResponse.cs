using System;

namespace SS14.Auth.Responses;

public sealed record QueryUserResponse(
    string UserName,
    Guid UserId,
    string? PatronTier,
    DateTimeOffset CreatedTime)
{
}