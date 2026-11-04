#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Tests.Utils;

public sealed class TestUserStore(ApplicationDbContext dbContext) : IUserPasswordStore<SpaceUser>
{
    public void Dispose()
    {
    }

    public Task<string> GetUserIdAsync(SpaceUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Id.ToString());
    }

    public Task<string?> GetUserNameAsync(SpaceUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.UserName);
    }

    public Task SetUserNameAsync(SpaceUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(SpaceUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.NormalizedUserName);
    }

    public Task SetNormalizedUserNameAsync(SpaceUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> CreateAsync(SpaceUser user, CancellationToken cancellationToken)
    {
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(SpaceUser user, CancellationToken cancellationToken)
    {
        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(SpaceUser user, CancellationToken cancellationToken)
    {
        dbContext.Users.Remove(user);

        var queuedDeletion = await dbContext.UserDeletionQueue
            .FirstOrDefaultAsync(e => e.SpaceUserId == user.Id, cancellationToken);

        if (queuedDeletion != null)
            dbContext.UserDeletionQueue.Remove(queuedDeletion);

        await dbContext.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public Task<SpaceUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        var id = Guid.Parse(userId);
        return dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public Task<SpaceUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        return dbContext.Users.FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken);
    }

    public Task SetPasswordHashAsync(SpaceUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(SpaceUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PasswordHash);
    }

    public Task<bool> HasPasswordAsync(SpaceUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PasswordHash != null);
    }
}
