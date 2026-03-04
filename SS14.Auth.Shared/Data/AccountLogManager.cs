using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Internal;

namespace SS14.Auth.Shared.Data;

#nullable enable

public sealed class AccountLogManager(
    ApplicationDbContext dbContext,
    ISystemClock systemClock,
    IHttpContextAccessor httpContextAccessor,
    SpaceUserManager userManager)
{
    public async Task LogNameChanged(
        SpaceUser user,
        string oldName,
        string newName,
        AccountLogActor? actor = null)
    {
        dbContext.PastAccountNames.Add(new PastAccountName
        {
            ChangeTime = systemClock.UtcNow.UtcDateTime,
            PastName = oldName,
            SpaceUser = user
        });

        await LogAndSave(user, new AccountLogUserNameChanged(newName, oldName), actor);
    }

    public async Task LogAndSave(SpaceUser targetUser, AccountLogEntry entry, AccountLogActor? actor = null)
    {
        await Log(targetUser, entry, actor);

        // Yeah I don't care about efficiency I wanna not forget to call this manually.
        await dbContext.SaveChangesAsync();
    }

    public async ValueTask Log(SpaceUser targetUser, AccountLogEntry entry, AccountLogActor? actor = null)
    {
        actor ??= await ActorFromCurrent();
        if (actor == null)
            throw new InvalidOperationException("Unable to determine actor for action!");

        dbContext.AccountLogs.Add(new AccountLog
        {
            SpaceUser = targetUser,
            Type = entry.Type,
            Data = JsonSerializer.SerializeToDocument(entry, entry.GetType()),
            Time = systemClock.UtcNow.UtcDateTime,
            Actor = actor.User,
            ActorAddress = actor.Address
        });
    }

    public async ValueTask<AccountLogActor?> ActorFromCurrent()
    {
        if (httpContextAccessor.HttpContext == null)
            return null;

        var user = await userManager.GetUserAsync(httpContextAccessor.HttpContext.User);
        if (user == null)
            return null;

        return ActorWithIP(user);
    }

    public AccountLogActor ActorWithIP(SpaceUser actor)
    {
        return new AccountLogActor(actor.Id, GetActorIP());
    }

    public AccountLogActor NoActor()
    {
        return new AccountLogActor(null, GetActorIP());
    }

    private IPAddress? GetActorIP()
    {
        if (httpContextAccessor.HttpContext == null)
            throw new InvalidOperationException("Unable to get HttpContext!");

        var address = httpContextAccessor.HttpContext.Connection.RemoteIpAddress;
        return address;
    }
}

public sealed record AccountLogActor(Guid? User, IPAddress? Address);
