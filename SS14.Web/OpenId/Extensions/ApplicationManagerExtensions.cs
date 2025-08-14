#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using SS14.Auth.Shared.Data;
using SS14.Web.Extensions;

namespace SS14.Web.OpenId.Extensions;

public static class ApplicationManagerExtensions
{
    public static async ValueTask<string?> GetFirstRedirectUrl(
        this OpenIddictApplicationManager<SpaceApplication> manager,
        SpaceApplication app)
    {
        var callbackUris = await manager.GetRedirectUrisAsync(app);
        return callbackUris.IsEmpty ? null : callbackUris[0];
    }

    public static async ValueTask<bool> GetRequiresPkceSetting(
        this OpenIddictApplicationManager<SpaceApplication> manager,
        SpaceApplication app)
    {
        var settings = await manager.GetSettingsAsync(app);
        return settings.ContainsKey(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);
    }

    public static async ValueTask<bool> GetPs256Setting(
        this OpenIddictApplicationManager<SpaceApplication> manager,
        SpaceApplication app)
    {
        var settings = await manager.GetSettingsAsync(app);
        return settings.TryGetValue(OpenIdConstants.SigningAlgorithmSetting, out var value)
               && value.Equals(OpenIddictConstants.Algorithms.RsaSsaPssSha256);
    }

    public static async ValueTask<List<SpaceApplication>> FindApplicationsByUserId(this OpenIddictApplicationManager<SpaceApplication> manager, Guid userId, CancellationToken cancel = default)
    {
        return await manager.ListAsync(x => x.Where(a => a.SpaceUserId == userId)
            , cancel).ToListAsync(ct: cancel);
    }
}
