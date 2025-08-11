
using System;

namespace SS14.Auth.Shared.Data;

// TODO: Delete
public sealed class UserOAuthClient
{
    public int UserOAuthClientId { get; set; }

    public Guid SpaceUserId { get; set; }
    public int ClientId { get; set; }

    public SpaceUser SpaceUser { get; set; }
    // TODO: Replace identityserver4 code in this file

    //public Client Client { get; set; }
}
