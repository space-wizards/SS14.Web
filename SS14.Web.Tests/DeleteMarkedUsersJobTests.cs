using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SS14.Auth.Jobs;
using SS14.Auth.Shared.Config;
using SS14.Auth.Shared.Data;
using SS14.Web.Tests.Utils;

namespace SS14.Web.Tests;

[TestFixture]
public sealed class TestDeleteMarkedUsersJob
{
    [Test]
    public async Task ExecuteDeletesOnlyExpiredQueuedUsersAndSendsDeletionEmails()
    {
        await using var dbContext = CreateDbContext();

        var expiredUser = new SpaceUser
        {
            Id = Guid.NewGuid(),
            UserName = "expired-user",
            NormalizedUserName = "EXPIRED-USER",
            Email = "expired@example.com",
            NormalizedEmail = "EXPIRED@EXAMPLE.COM",
            EmailConfirmed = true,
            CreatedTime = DateTimeOffset.UtcNow
        };

        var recentUser = new SpaceUser
        {
            Id = Guid.NewGuid(),
            UserName = "recent-user",
            NormalizedUserName = "RECENT-USER",
            Email = "recent@example.com",
            NormalizedEmail = "RECENT@EXAMPLE.COM",
            EmailConfirmed = true,
            CreatedTime = DateTimeOffset.UtcNow,
        };

        dbContext.Users.AddRange(expiredUser, recentUser);
        dbContext.UserDeletionQueue.AddRange(
            new UserDeletionQueueEntry
            {
                SpaceUserId = expiredUser.Id,
                QueuedOn = DateTime.UtcNow.AddDays(-31),
            },
            new UserDeletionQueueEntry
            {
                SpaceUserId = recentUser.Id,
                QueuedOn = DateTime.UtcNow.AddDays(-1),
            });

        await dbContext.SaveChangesAsync();

        var rawEmailSender = new RecordingEmailSender();
        var userManager = CreateUserManager(dbContext);
        var job = new DeleteMarkedUsersJob(
            dbContext,
            Options.Create(new AccountConfiguration { UserDeletionGracePeriodDays = 30 }),
            userManager,
            rawEmailSender,
            NullLogger<DeleteMarkedUsersJob>.Instance);

        await job.Execute(new TestJobExecutionContext());

        Assert.Multiple(() =>
        {
            Assert.That(dbContext.Users.Any(u => u.Id == expiredUser.Id), Is.False);
            Assert.That(dbContext.Users.Any(u => u.Id == recentUser.Id), Is.True);

            Assert.That(dbContext.DeletedUserIds.Any(u => u.SpaceUserId == expiredUser.Id), Is.True);
            Assert.That(dbContext.DeletedUserIds.Any(u => u.SpaceUserId == recentUser.Id), Is.False);

            Assert.That(dbContext.UserDeletionQueue.Any(e => e.SpaceUserId == expiredUser.Id), Is.False);
            Assert.That(dbContext.UserDeletionQueue.Any(e => e.SpaceUserId == recentUser.Id), Is.True);

            Assert.That(
                rawEmailSender.SentEmails.Any(e =>
                    e.Email == "expired@example.com" &&
                    e.Subject == "Account deleted"),
                Is.True);

            Assert.That(
                rawEmailSender.SentEmails.Any(e =>
                    e.Email == "support@spacestation14.com" &&
                    e.Subject == "Processed account deletions" &&
                    e.HtmlMessage.Contains(expiredUser.Id.ToString())),
                Is.True);

            Assert.That(
                rawEmailSender.SentEmails.Any(e => e.Email == "recent@example.com"),
                Is.False);
        });
    }

    private static TestApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestApplicationDbContext(options);
    }

    private static SpaceUserManager CreateUserManager(ApplicationDbContext dbContext)
    {
        var store = new TestUserStore(dbContext);

        return new SpaceUserManager(
            dbContext,
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<SpaceUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<SpaceUser>>.Instance,
            Options.Create(new AccountConfiguration { UserDeletionGracePeriodDays = 30 }));
    }
}
