
using System;
using IdentityServer4.EntityFramework.Entities;

namespace SS14.Auth.Shared.Data;

public sealed class UserOAuthClient
{
    public int UserOAuthClientId { get; set; }
    
    public Guid SpaceUserId { get; set; }
    public int ClientId { get; set; }
    
    public SpaceUser SpaceUser { get; set; }
    public Client Client { get; set; }
}