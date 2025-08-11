#nullable enable
using System;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using SS14.Auth.Shared.Data;
using SS14.ServerHub.Shared.Data;

namespace SS14.Web.Data;

/// <summary>
/// Handles writing audit log entries for the hub audit log.
/// </summary>
/// <remarks>
/// The function that add individual log records do not explicitly save the database themselves.
/// You should call <c>SaveChangedAsync</c> on <see cref="HubDbContext"/> manually.
/// </remarks>
public sealed class HubAuditLogManager
{
    private readonly HubDbContext _dbContext;
    private readonly SpaceUserManager _userManager;
    private readonly ISystemClock _systemClock;

    public HubAuditLogManager(HubDbContext dbContext, SpaceUserManager userManager, ISystemClock systemClock)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _systemClock = systemClock;
    }

    public void Log(ClaimsPrincipal actor, HubAuditEntry entry)
    {
        var id = _userManager.GetUserId(actor);
        if (id == null)
            throw new InvalidOperationException("User does not have a valid ID!");
        
        Log(Guid.Parse(id), entry);
    }
    
    public void Log(SpaceUser actor, HubAuditEntry entry)
    {
        Log(actor.Id, entry);
    }

    public void Log(Guid actor, HubAuditEntry entry)
    {
        _dbContext.HubAudit.Add(new HubAudit
        {
            Actor = actor,
            Type = entry.Type,
            Data = JsonSerializer.SerializeToDocument(entry, entry.GetType()),
            Time = _systemClock.UtcNow.UtcDateTime
        });
    }
}