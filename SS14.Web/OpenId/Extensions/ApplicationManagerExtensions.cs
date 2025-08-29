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

    public static async ValueTask<bool> GetRequiresPkce(
        this OpenIddictApplicationManager<SpaceApplication> manager,
        SpaceApplication app)
    {
        var requirements = await manager.GetRequirementsAsync(app);
        return requirements.Contains(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);
    }

    public static async ValueTask<bool> GetPs256Setting(
        this OpenIddictApplicationManager<SpaceApplication> manager,
        SpaceApplication app)
    {
        return await manager.CheckSetting(app, OpenIdConstants.SigningAlgorithmSetting, OpenIddictConstants.Algorithms.RsaSsaPssSha256);
    }

    public static async ValueTask<bool> GetAllowPlainPkceSetting(
        this OpenIddictApplicationManager<SpaceApplication> manager,
        SpaceApplication app)
    {
        return await manager.CheckSetting(app, OpenIdConstants.AllowPlainPkce, "true");
    }

    public static async ValueTask<bool> IsDisabled(
        this OpenIddictApplicationManager<SpaceApplication> manager,
        SpaceApplication app)
    {
        return await manager.CheckSetting(app, OpenIdConstants.DisabledSetting, "true");
    }

    public static async ValueTask<bool> CheckSetting(this OpenIddictApplicationManager<SpaceApplication> manager,
        SpaceApplication app,
        string setting,
        string comparand)
    {
        var settings = await manager.GetSettingsAsync(app);
        return settings.TryGetValue(setting, out var value)
               && value.Equals(comparand);
    }

    public static async ValueTask<List<SpaceApplication>> FindApplicationsByUserId(this OpenIddictApplicationManager<SpaceApplication> manager, Guid userId, CancellationToken cancel = default)
    {
        return await manager.ListAsync(x => x.Where(a => a.SpaceUserId == userId)
            , cancel).ToListAsync(ct: cancel);
    }

    public static async ValueTask<int> GetAccessTokenLifetime(
        this OpenIddictApplicationManager<SpaceApplication> manager,
        SpaceApplication app)
    {
        return await GetLifetime(manager, app, OpenIddictConstants.Settings.TokenLifetimes.AccessToken);
    }

    public static async ValueTask<int> GetIdentityTokenLifetime(
        this OpenIddictApplicationManager<SpaceApplication> manager,
        SpaceApplication app)
    {
        return await GetLifetime(manager, app, OpenIddictConstants.Settings.TokenLifetimes.IdentityToken);
    }

    public static async ValueTask<int> GetRefreshTokenLifetime(
        this OpenIddictApplicationManager<SpaceApplication> manager,
        SpaceApplication app)
    {
        return await GetLifetime(manager, app, OpenIddictConstants.Settings.TokenLifetimes.RefreshToken);
    }

    private static async ValueTask<int> GetLifetime(OpenIddictApplicationManager<SpaceApplication> manager,
        SpaceApplication app,
        string settingName)
    {
        var settings = await manager.GetSettingsAsync(app);
        return int.Parse(settings.GetValueOrDefault(settingName, "0"));
    }
}
