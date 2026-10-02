using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using SS14.Auth.Shared.Config;
using SS14.Auth.Shared.Data;

namespace SS14.Auth.Jobs;

public sealed class DeleteMarkedUsersJob(
    ApplicationDbContext dbContext,
    IOptions<AccountConfiguration> configuration,
    UserManager<SpaceUser> userManager,
    ILogger<CleanOldSessionsJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var gracePeriod = configuration.Value.UserDeletionGracePeriodDays;

        var entries = await dbContext.UserDeletionQueue
            .Where(e => e.QueuedOn < DateTime.UtcNow.AddDays(-gracePeriod))
            .ToListAsync(context.CancellationToken);
        foreach (var entry in entries)
        {
            var user = await userManager.FindByIdAsync(entry.SpaceUserId.ToString());
            if (user == null)
            {
                logger.LogError("User {Id} marked for deletion was not found in the database.", entry.SpaceUserId);
                dbContext.UserDeletionQueue.Remove(entry);
                continue;
            }

            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
                logger.LogError("Failed to delete user {Id} marked for deletion.", entry.SpaceUserId);
        }

        await dbContext.SaveChangesAsync();
    }
}
