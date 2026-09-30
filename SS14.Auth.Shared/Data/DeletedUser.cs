using System;

namespace SS14.Auth.Shared.Data;

public sealed  class DeletedUser
{
    public Guid SpaceUserId { get; set; }
    public DateTime DeletedOn { get; set; }
}
