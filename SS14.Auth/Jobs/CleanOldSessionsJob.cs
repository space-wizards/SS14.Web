using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using SS14.Auth.Shared.Data;

namespace SS14.Auth.Jobs;

/// <summary>
/// Periodically clean expired <see cref="LoginSession"/> objects from the database.
/// </summary>
public sealed class CleanOldSessionsJob(ApplicationDbContext dbContext, ILogger<CleanOldSessionsJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        // TODO: Use ExecuteDelete() on newer EF Core version.
        var toDelete = await dbContext.ActiveSessions
            .Where(x => x.Expires < DateTimeOffset.UtcNow)
            .ToListAsync(context.CancellationToken);

        dbContext.RemoveRange(toDelete);
        await dbContext.SaveChangesAsync(context.CancellationToken);
        logger.LogInformation("Cleared {RemovedSessions} old sessions from database", toDelete.Count);
    }
}