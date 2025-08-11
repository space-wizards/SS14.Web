#nullable enable
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SS14.Auth.Shared.Data;
using SS14.WebEverythingShared;

namespace SS14.Web;

/// <summary>
/// Helper class that wraps up all the personal data for a user into a neat little zip file!
/// </summary>
public sealed class PersonalDataCollector(ApplicationDbContext dbContext, ILogger<PersonalDataCollector> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters =
        {
            new IPAddressJsonConverter(),
        },
    };

    public async Task<Stream> CollectPersonalData(SpaceUser user, CancellationToken cancel)
    {
        logger.LogInformation("Collecting personal data for {UserName} ({UserId})", user.UserName, user.Id);

        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            CollectAspNetUsers(zip, user);
            await CollectAccountLogs(zip, user, cancel);
            await CollectAspNetUserRoles(zip, user, cancel);
            await CollectPastAccountNames(zip, user, cancel);
            await CollectPatrons(zip, user, cancel);
            await CollectUserOAuthClients(zip, user, cancel);
            await CollectIS4PersistedGrants(zip, user, cancel);
            await CollectHwidUsers(zip, user, cancel);
        }

        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    private static void CollectAspNetUsers(ZipArchive archive, SpaceUser user)
    {
        SerializeToFile<SpaceUserData[]>(
            archive,
            "AspNetUsers.json",
            [
                new SpaceUserData(
                    user.Id,
                    user.UserName,
                    user.CreatedTime,
                    user.EmailConfirmed,
                    user.NormalizedEmail,
                    user.TwoFactorEnabled,
                    user.NormalizedUserName,
                    user.AdminLocked,
                    user.AdminNotes,
                    user.LastUsernameChange
                ),
            ]);
    }

    private async Task CollectAccountLogs(ZipArchive zip, SpaceUser user, CancellationToken cancel)
    {
        // EF Core hints in this function are Rider being buggy.

        // ReSharper disable once EntityFramework.NPlusOne.IncompleteDataQuery
        var logs = await dbContext.AccountLogs.Where(x => x.SpaceUser == user).ToListAsync(cancel);

        var toSendLogs = logs.Select(log =>
            {
                // Hide actor information from hub admins.
                var actor = log.Actor;
                var actorAddress = log.ActorAddress;
                if (actor != user.Id)
                {
                    actor = null;
                    actorAddress = null;
                }

                return new AccountLog
                {
                    Id = log.Id,
                    Actor = actor,
                    // ReSharper disable once EntityFramework.NPlusOne.IncompleteDataUsage
                    Data = log.Data,
                    Time = log.Time,
                    Type = log.Type,
                    ActorAddress = actorAddress,
                    SpaceUserId = log.SpaceUserId,
                };
            })
            .ToArray();

        SerializeToFile(zip, "AccountLogs.json", toSendLogs);
    }

    private async Task CollectAspNetUserRoles(ZipArchive zip, SpaceUser user, CancellationToken cancel)
    {
        var roles = await dbContext.UserRoles.Where(userRole => userRole.UserId == user.Id).ToListAsync(cancel);

        SerializeToFile(zip, "AspNetUserRoles.json", roles);
    }

    private async Task CollectPastAccountNames(ZipArchive zip, SpaceUser user, CancellationToken cancel)
    {
        var names = await dbContext.PastAccountNames.Where(pastName => pastName.SpaceUser == user).ToListAsync(cancel);

        SerializeToFile(zip, "PastAccountNames.json", names);
    }

    private async Task CollectPatrons(ZipArchive zip, SpaceUser user, CancellationToken cancel)
    {
        var patrons = await dbContext.Patrons.Where(pastName => pastName.SpaceUser == user).ToListAsync(cancel);

        SerializeToFile(zip, "Patrons.json", patrons);
    }

    private async Task CollectUserOAuthClients(ZipArchive zip, SpaceUser user, CancellationToken cancel)
    {
        var dbConnection = dbContext.Database.GetDbConnection();
        var data = await dbConnection.QuerySingleAsync<string>("""
            SELECT
                COALESCE(json_agg(to_jsonb(data)), '[]') #>> '{}'
            FROM (
                SELECT
                    *,
                    (SELECT to_jsonb(client) FROM (
                        SELECT
                            *,
                            (SELECT COALESCE(json_agg(to_jsonb(client_scopeq)), '[]') FROM (
                                SELECT * FROM "IS4"."ClientScopes" cs WHERE cs."ClientId" = c."Id"
                            ) client_scopeq)
                            as "Scopes",
                            (SELECT COALESCE(json_agg(to_jsonb(client_redirectq)), '[]') FROM (
                                SELECT * FROM "IS4"."ClientRedirectUris" cru WHERE cru."ClientId" = c."Id"
                            ) client_redirectq)
                            as "RedirectUris",
                            (SELECT COALESCE(json_agg(to_jsonb(client_grantq)), '[]') FROM (
                                SELECT * FROM "IS4"."ClientGrantTypes" cgt WHERE cgt."ClientId" = c."Id"
                            ) client_grantq)
                            as "GrantTypes"
                        FROM
                            "IS4"."Clients" c
                        WHERE c."Id" = a."ClientId"
                    ) client)
                    as "Client"
                FROM
                    "UserOAuthClients" a
                WHERE
                    "SpaceUserId" = @UserId
            ) as data
            """,
            new
            {
                UserId = user.Id,
            });

        StringToFile(zip, "UserOAuthClients.json", data);
    }

    private async Task CollectIS4PersistedGrants(ZipArchive zip, SpaceUser user, CancellationToken cancel)
    {
        // TODO: Replace identityserver4 code in this file

        //var grants = await dbContext.PersistedGrants.Where(x => x.SubjectId == user.Id.ToString()).ToListAsync(cancel);

        //SerializeToFile(zip, "IS4.PersistedGrants.json", grants);
    }

    private async Task CollectHwidUsers(ZipArchive zip, SpaceUser user, CancellationToken cancel)
    {
        var hwidUsers = await dbContext.HwidUsers
            .Include(h => h.Hwid)
            .Where(h => h.SpaceUser == user)
            .ToListAsync(cancel);

        SerializeToFile(zip, "HwidUsers.json", hwidUsers);
    }

    private static void StringToFile(ZipArchive archive, string name, string data)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var stream = entry.Open();
        stream.Write(Encoding.UTF8.GetBytes(data));
    }

    private static void SerializeToFile<T>(ZipArchive archive, string name, T data) where T : notnull
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var stream = entry.Open();
        JsonSerializer.Serialize(stream, data, JsonOptions);
    }

    [UsedImplicitly]
    private sealed record SpaceUserData(
        Guid Id,
        string UserName,
        DateTimeOffset CreatedTime,
        bool EmailConfirmed,
        string NormalizedEmail,
        bool TwoFactorEnabled,
        string NormalizedUserName,
        bool AdminLocked,
        string AdminNotes,
        DateTimeOffset? LastUsernameChange);
}
