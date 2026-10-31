using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using SS14.Auth.Shared;
using SS14.Auth.Shared.Config;
using SS14.Auth.Shared.Data;
using SS14.Auth.Shared.Emails;

namespace SS14.Auth.Jobs;

[UsedImplicitly]
public sealed class DeleteMarkedUsersJob(
    ApplicationDbContext dbContext,
    IOptions<AccountConfiguration> configuration,
    UserManager<SpaceUser> userManager,
    IEmailSender emailSender,
    ILogger<DeleteMarkedUsersJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var gracePeriod = configuration.Value.UserDeletionGracePeriodDays;

        var entries = await dbContext.UserDeletionQueue
            .Where(e => e.QueuedOn < DateTime.UtcNow.AddDays(-gracePeriod))
            .ToListAsync(context.CancellationToken);

        var deletedAccounts = new List<(Guid, string?, string?)>();

        foreach (var entry in entries)
        {
            var user = await userManager.FindByIdAsync(entry.SpaceUserId.ToString());
            if (user == null)
            {
                logger.LogError("User {Id} marked for deletion was not found in the database.", entry.SpaceUserId);
                dbContext.UserDeletionQueue.Remove(entry);
                continue;
            }

            var deletedAccount = (entry.SpaceUserId, user.UserName, user.Email);
            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                logger.LogError("Failed to delete user {Id} marked for deletion.", entry.SpaceUserId);
                await ModelShared.SendDeletionFailedEmail(emailSender, entry.SpaceUserId);
                return;
            }

            deletedAccounts.Add(deletedAccount);
        }

        await dbContext.SaveChangesAsync();

        if (deletedAccounts.Count == 0)
            return;

        await ModelShared.SendUserDeletionQueueEmail(emailSender, deletedAccounts);
        foreach (var (_, _, email) in deletedAccounts)
        {
            if (email == null)
                continue;

            await ModelShared.SendUserDeletedEmail(emailSender, email);
        }
    }
}
