using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using SS14.Auth.Shared.Config;
using SS14.Auth.Shared.Data;

namespace SS14.Auth.Jobs;

public sealed class DeleteUnconfirmedAccounts(ApplicationDbContext dbContext,
    ILogger<DeleteUnconfirmedAccounts> logger,
    IOptions<AccountConfiguration> configuration) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var optionsValue = configuration.Value;

        var retainMinimum = DateTime.UtcNow - TimeSpan.FromDays(optionsValue.DeleteUnconfirmedAfter);

        // TODO: Use ExecuteDelete() on newer EF Core version.
        var accounts = await dbContext.Users
            .Where(user => !user.EmailConfirmed && user.CreatedTime < retainMinimum)
            .ToListAsync(context.CancellationToken);

        dbContext.RemoveRange(accounts);
        await dbContext.SaveChangesAsync();
        logger.LogInformation("Deleted {Count} unconfirmed accounts from database", accounts.Count);
    }
}