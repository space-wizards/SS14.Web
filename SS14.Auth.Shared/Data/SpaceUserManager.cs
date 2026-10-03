using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SS14.Auth.Shared.Config;

namespace SS14.Auth.Shared.Data;

[UsedImplicitly]
public sealed class SpaceUserManager(
    ApplicationDbContext dbContext,
    IUserStore<SpaceUser> store,
    IOptions<IdentityOptions> optionsAccessor,
    IPasswordHasher<SpaceUser> passwordHasher,
    IEnumerable<IUserValidator<SpaceUser>> userValidators,
    IEnumerable<IPasswordValidator<SpaceUser>> passwordValidators,
    ILookupNormalizer keyNormalizer,
    IdentityErrorDescriber errors,
    IServiceProvider services,
    ILogger<UserManager<SpaceUser>> logger, IOptions<AccountConfiguration> accountConfig)
    : UserManager<SpaceUser>(
        store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer,
        errors, services, logger)
{
    private readonly IServiceProvider _services = services;

    public override async Task<IdentityResult> CreateAsync(SpaceUser user)
    {
        var result = await base.CreateAsync(user);
        if (!result.Succeeded)
            return result;

        // We can't directly depend on AccountLogManager due to a circular dependency.
        // Whoopsie.
        var accountLogManager = _services.GetRequiredService<AccountLogManager>();
        await accountLogManager.LogAndSave(user, new AccountLogCreated(), accountLogManager.NoActor());
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

    /// <summary>
    /// Adds the specified user to the deletion queue.
    /// </summary>
    /// <param name="user">The user to be queued for deletion.</param>
    public void QueueDeletion(SpaceUser user)
    {
        dbContext.UserDeletionQueue.Add(new UserDeletionQueueEntry { SpaceUserId = user.Id, QueuedOn = DateTime.UtcNow });
        dbContext.SaveChanges();
    }

    /// <summary>
    /// Removes the specified user from the deletion queue if they are currently queued for deletion.
    /// </summary>
    /// <param name="user">The user to be removed from the deletion queue.</param>
    public void CancelQueuedDeletion(SpaceUser user)
    {
        var entry = dbContext.UserDeletionQueue.FirstOrDefault(x => x.SpaceUserId == user.Id);
        if (entry == null)
            return;

        dbContext.UserDeletionQueue.Remove(entry);
        dbContext.SaveChanges();
    }

    ///<inheritdoc />
    /// <remarks>
    /// Stores the deleted user in the DeletedUserIds table.
    /// </remarks>
    public override async  Task<IdentityResult> DeleteAsync(SpaceUser user)
    {
        dbContext.DeletedUserIds.Add(new DeletedUser { SpaceUserId = user.Id, DeletedOn = DateTime.UtcNow });
        return await base.DeleteAsync(user);
    }

    /// <summary>
    /// Retrieves the remaining time until the user associated with the specified principal is deleted, if they are queued for deletion.
    /// </summary>
    /// <param name="principal">The claims principal representing the user.</param>
    /// <returns>
    /// A <see cref="TimeSpan"/> indicating the time remaining until deletion, or <c>null</c> if the user
    /// is not queued for deletion or does not exist.
    /// </returns>
    public async Task<TimeSpan?> GetDaysUntilDeletionAsync(ClaimsPrincipal principal)
    {
        var user = await GetUserAsync(principal);
        if (user == null)
            return null;

        var entry = dbContext.UserDeletionQueue.FirstOrDefault(x => x.SpaceUserId == user.Id);
        if (entry == null)
            return null;

        var gracePeriod = accountConfig.Value.UserDeletionGracePeriodDays;
        return TimeSpan.FromDays(gracePeriod) - (DateTime.UtcNow - entry.QueuedOn);
    }
}
