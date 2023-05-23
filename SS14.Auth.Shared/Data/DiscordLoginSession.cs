using System;

namespace SS14.Auth.Shared.Data;

public class DiscordLoginSession
{
    public Guid Id { get; set; }

    public Guid SpaceUserId { get; set; }
    public SpaceUser SpaceUser { get; set; }

    public DateTimeOffset Expires { get; set; }
}
