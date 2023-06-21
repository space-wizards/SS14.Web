using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SS14.Auth.Shared.Data;

[UsedImplicitly]
public sealed class SpaceUserManager : UserManager<SpaceUser>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ISystemClock _systemClock;

    public SpaceUserManager(
        ApplicationDbContext dbContext,
        ISystemClock systemClock,
        IUserStore<SpaceUser> store,
        IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<SpaceUser> passwordHasher,
        IEnumerable<IUserValidator<SpaceUser>> userValidators,
        IEnumerable<IPasswordValidator<SpaceUser>> passwordValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        IServiceProvider services,
        ILogger<UserManager<SpaceUser>> logger) : base(
        store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services,
        logger)
    {
        _dbContext = dbContext;
        _systemClock = systemClock;
    }

    public override async Task<IdentityResult> CreateAsync(SpaceUser user)
    {
        var result = await base.CreateAsync(user);
        if (!result.Succeeded)
            return result;
        
        AccountLog(user, AccountLogType.Created, new AccountLogCreated());
        return result;
    }
    
    public async Task<SpaceUser> FindByNameOrEmailAsync(string nameOrEmail)
    {
        var user = await FindByNameAsync(nameOrEmail);
        if (user != null)
        {
            return user;
        }

        return await FindByEmailAsync(nameOrEmail);
    }

    public void LogNameChanged(
        SpaceUser user,
        string oldName,
        string newName,
        SpaceUser actor)
    {
        _dbContext.PastAccountNames.Add(new PastAccountName
        {
            ChangeTime = _systemClock.UtcNow.UtcDateTime,
            PastName = oldName,
            SpaceUser = user
        });

        AccountLog(
            user,
            AccountLogType.UserNameChanged,
            new AccountLogUserNameChanged(newName, oldName, actor.Id));
    }

    public void LogEmailChanged(SpaceUser user, string oldEmail, string newEmail, SpaceUser actor)
    {
        AccountLog(
            user, 
            AccountLogType.EmailChanged,
            new AccountLogEmailChanged(oldEmail, newEmail, actor.Id));
    }
    
    public void LogPasswordChanged(SpaceUser user, SpaceUser actor)
    {
        AccountLog(
            user, 
            AccountLogType.PasswordChanged,
            new AccountLogPasswordChanged(actor.Id));
    }

    public void LogEmailConfirmedChanged(SpaceUser user, bool newEmailConfirmed, SpaceUser actor)
    {
        AccountLog(
            user, 
            AccountLogType.EmailConfirmedChanged,
            new AccountLogEmailConfirmedChanged(newEmailConfirmed, actor.Id));
    }

    public void LogPatreonLinked(SpaceUser user, SpaceUser actor)
    {
        AccountLog(
            user, 
            AccountLogType.PatreonLinked,
            new AccountLogPatreonLinked(actor.Id));
    }
    
    public void LogPatreonUnlinked(SpaceUser user, SpaceUser actor)
    {
        AccountLog(
            user, 
            AccountLogType.PatreonUnlinked,
            new AccountLogPatreonUnlinked(actor.Id));
    }

    public void LogAdminNotesChanged(SpaceUser user, string newNotes, SpaceUser actor)
    {
        AccountLog(
            user, 
            AccountLogType.AdminNotesChanged,
            new AccountLogAdminNotesChanged(newNotes, actor.Id));
    }
    
    public void LogAdminLockedChanged(SpaceUser user, bool newLocked, SpaceUser actor)
    {
        AccountLog(
            user, 
            AccountLogType.AdminLockedChanged,
            new AccountLogAdminLockedChanged(newLocked, actor.Id));
    }

    public void LogAuthRoleAdded(SpaceUser user, Guid role, SpaceUser actor)
    {
        AccountLog(
            user, 
            AccountLogType.AuthRoleAdded,
            new AccountLogAuthRoleAdded(role, actor.Id));
    }
    
    public void LogAuthRoleRemoved(SpaceUser user, Guid role, SpaceUser actor)
    {
        AccountLog(
            user, 
            AccountLogType.AuthRoleRemoved,
            new AccountLogAuthRoleRemoved(role, actor.Id));
    }

    public void AccountLog(SpaceUser user, AccountLogType type, AccountLogEntry entry)
    {
        _dbContext.AccountLogs.Add(new AccountLog
        {
            SpaceUser = user,
            Type = type,
            Data = JsonSerializer.SerializeToDocument(entry, entry.GetType()),
            Time = _systemClock.UtcNow.UtcDateTime,
        });
    }
}