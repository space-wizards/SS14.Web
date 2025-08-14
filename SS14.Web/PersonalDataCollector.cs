#nullable enable
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenIddict.Core;
using SS14.Auth.Shared.Data;
using SS14.Web.Extensions;
using SS14.Web.OpenId.Extensions;
using SS14.WebEverythingShared;

namespace SS14.Web;

/// <summary>
/// Helper class that wraps up all the personal data for a user into a neat little zip file!
/// </summary>
public sealed class PersonalDataCollector(
    ApplicationDbContext dbContext,
    OpenIddictApplicationManager<SpaceApplication> applicationManager,
    OpenIddictAuthorizationManager<OpeniddictDefaultTypes.DefaultAuthorization> authorizationManager,
    OpenIddictTokenManager<OpeniddictDefaultTypes.DefaultToken> tokenManager,
    ILogger<PersonalDataCollector> logger)
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
            await CollectAuthorizations(zip, user, cancel);
            await CollectTokens(zip, user, cancel);
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
        var applications = await applicationManager.FindApplicationsByUserId(user.Id, cancel);

        var data = applications.Select(x => new SpaceApplicationData(
           Id: x.Id,
           UserId: x.SpaceUserId,
           ClientId: x.ClientId,
           ClientType: x.ClientType,
           ApplicationType: x.ApplicationType,
           LogoUri: x.LogoUri,
           DisplayName: x.DisplayName,
           Permissions: x.Permissions,
           PostLogoutRedirectUris: x.PostLogoutRedirectUris,
           RedirectUris: x.RedirectUris,
           Properties: x.Properties,
           Requirements: x.Requirements,
           Settings: x.Settings,
           HomePageUrl: x.WebsiteUrl
       ));

       SerializeToFile(zip, "OAuthClients.json", data);
    }

    private async Task CollectAuthorizations(ZipArchive zip, SpaceUser user, CancellationToken cancel)
    {
        var authorizations = await authorizationManager.FindBySubjectAsync(user.Id.ToString(), cancel)
            .ToListAsync(ct: cancel);

        var data = authorizations.Select(x => new AuthorizationData(
            Id: x.Id,
            Subject: x.Subject,
            ApplicationId: x.Application?.Id,
            ApplicationName: x.Application?.DisplayName,
            Type: x.Type,
            Status: x.Status,
            CreationDate: x.CreationDate,
            Scopes: x.Scopes
            ));

        SerializeToFile(zip, "OAuthAuthorizations.json", data);
    }

    private async Task CollectTokens(ZipArchive zip, SpaceUser user, CancellationToken cancel)
    {
        var tokens = await tokenManager.FindBySubjectAsync(user.Id.ToString(), cancel)
            .ToListAsync(ct: cancel);

        var data = tokens.Select(x => new TokenData(
            Id: x.Id,
            Subject: x.Subject,
            AuthorizationId: x.Authorization?.Id,
            ApplicationId: x.Application?.Id,
            ApplicationName: x.Application?.DisplayName,
            Status: x.Status,
            Type: x.Type,
            ReferenceId: x.ReferenceId,
            Payload: x.Payload,
            CreationDate: x.CreationDate,
            ExpirationDate: x.ExpirationDate,
            RedemptionDate: x.RedemptionDate
        ));

        SerializeToFile(zip, "OAuthTokens.json", data);
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
        string? UserName,
        DateTimeOffset CreatedTime,
        bool EmailConfirmed,
        string? NormalizedEmail,
        bool TwoFactorEnabled,
        string? NormalizedUserName,
        bool AdminLocked,
        string AdminNotes,
        DateTimeOffset? LastUsernameChange);

    [UsedImplicitly]
    private sealed record SpaceApplicationData(
        string? Id,
        Guid? UserId,
        string? ClientId,
        string? ClientType,
        string? ApplicationType,
        string? LogoUri,
        string? DisplayName,
        string? Permissions,
        string? PostLogoutRedirectUris,
        string? RedirectUris,
        string? HomePageUrl,
        string? Properties,
        string? Requirements,
        string? Settings);

    [UsedImplicitly]
    private sealed record AuthorizationData(
        string? Id,
        string? Subject,
        string? ApplicationId,
        string? ApplicationName,
        string? Type,
        string? Status,
        DateTime? CreationDate,
        string? Scopes
        );

    [UsedImplicitly]
    private sealed record TokenData(
        string? Id,
        string? Subject,
        string? AuthorizationId,
        string? ApplicationId,
        string? ApplicationName,
        string? Status,
        string? Type,
        string? ReferenceId,
        string? Payload,
        DateTime? CreationDate,
        DateTime? ExpirationDate,
        DateTime? RedemptionDate
    );
}
