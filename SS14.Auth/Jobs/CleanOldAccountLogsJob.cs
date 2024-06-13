using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using SS14.Auth.Shared.Config;
using SS14.Auth.Shared.Data;

namespace SS14.Auth.Jobs;

public sealed class CleanOldAccountLogsJob(
    ApplicationDbContext dbContext,
    ILogger<CleanOldAccountLogsJob> logger,
    IOptions<AccountLogRetentionConfiguration> configuration)
    : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var optionsValue = configuration.Value;

        await DeleteOldLogs(optionsValue, context.CancellationToken);
        await DeleteOldIPs(optionsValue, context.CancellationToken);

        await dbContext.SaveChangesAsync(context.CancellationToken);
    }

    private async Task DeleteOldLogs(AccountLogRetentionConfiguration optionsValue, CancellationToken cancel)
    {
        var totalCount = 0L;

        foreach (var group in AccountLogEntry.EnumToRetention.GroupBy(x => x.Value))
        {
            var retention = group.Key;
            var retentionTime = retention switch
            {
                AccountLogRetainType.Detail => optionsValue.DetailRetainDays,
                AccountLogRetainType.AccountManagement => optionsValue.AccountManagementRetainDays,
                _ => int.MaxValue
            };

            if (retentionTime == int.MaxValue)
                continue;

            var retainMinimum = DateTime.UtcNow - TimeSpan.FromDays(retentionTime);
            var logTypes = group.Select(x => x.Key).ToList();
            // TODO: Use ExecuteDelete() on newer EF Core version.
            var logs = await dbContext.AccountLogs
                .Where(log => log.Time < retainMinimum && logTypes.Contains(log.Type))
                .ToListAsync(cancel);

            totalCount += logs.Count;

            dbContext.RemoveRange(logs);
        }

        logger.LogInformation("Deleted {Count} old logs", totalCount);
    }

    private async Task DeleteOldIPs(AccountLogRetentionConfiguration optionsValue, CancellationToken cancel)
    {
        // TODO: Use ExecuteUpdate() on newer EF Core version.
        var retainMinimum = DateTime.UtcNow - TimeSpan.FromDays(optionsValue.IPRetainDays);
        var logs = await dbContext.AccountLogs
            .Where(log => log.Time < retainMinimum && log.ActorAddress != null)
            .ToListAsync(cancellationToken: cancel);

        foreach (var accountLog in logs)
        {
            accountLog.ActorAddress = null;
        }

        logger.LogInformation("Deleted IPs from {Count} logs", logs.Count);
    }
}