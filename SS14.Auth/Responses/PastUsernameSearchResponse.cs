using System;

namespace SS14.Auth.Responses;

public sealed record PastUsernameSearchResponse(
    string PresentUsername,
    Guid UserId
    );
