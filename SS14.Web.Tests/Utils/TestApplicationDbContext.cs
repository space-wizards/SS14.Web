using Microsoft.EntityFrameworkCore;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Tests.Utils;

/// <summary>
/// Provides a specialized implementation of <see cref="ApplicationDbContext"/> for testing purposes.
/// This context modifies the default entity configuration specific to tests by ignoring certain entities
/// and properties that are not relevant in the test scenarios.
/// This also ensures json properties don't break tests
/// </summary>
public sealed class TestApplicationDbContext(DbContextOptions<TestApplicationDbContext> options) : ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<SpaceUser>()
            .Ignore(x => x.AccountLogs);

        builder.Ignore<AccountLog>();
    }
}
