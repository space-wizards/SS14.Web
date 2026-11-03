#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using SS14.Auth.Shared.Data;

namespace SS14.Web.OpenId.Extensions;

public static class ApplicationManagerExtensions
{
    extension(OpenIddictApplicationManager<SpaceApplication> manager)
    {
        public async ValueTask<string?> GetFirstRedirectUrl(SpaceApplication app)
        {
            var callbackUris = await manager.GetRedirectUrisAsync(app);
            return callbackUris.IsEmpty ? null : callbackUris[0];
        }

        public async ValueTask<bool> GetRequiresPkce(SpaceApplication app)
        {
            var requirements = await manager.GetRequirementsAsync(app);
            return requirements.Contains(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);
        }

        public async ValueTask<bool> GetPs256Setting(SpaceApplication app)
        {
            return await manager.CheckSetting(app, OpenIdConstants.SigningAlgorithmSetting, OpenIddictConstants.Algorithms.RsaSsaPssSha256);
        }

        public async ValueTask<bool> GetAllowPlainPkceSetting(SpaceApplication app)
        {
            return await manager.CheckSetting(app, OpenIdConstants.AllowPlainPkce, "true");
        }

        public async ValueTask<bool> IsDisabled(SpaceApplication app)
        {
            return await manager.CheckSetting(app, OpenIdConstants.DisabledSetting, "true");
        }

        private async ValueTask<bool> CheckSetting(SpaceApplication app,
            string setting,
            string comparand)
        {
            var settings = await manager.GetSettingsAsync(app);
            return settings.TryGetValue(setting, out var value)
                   && value.Equals(comparand);
        }

        public async ValueTask<List<SpaceApplication>> FindApplicationsByUserId(Guid userId, CancellationToken cancel = default)
        {
            return await manager
                .ListAsync(x => x.Where(a => a.SpaceUserId == userId), cancel)
                .ToListAsync(cancellationToken: cancel);
        }

        public async ValueTask<int> GetAccessTokenLifetime(SpaceApplication app)
        {
            return await GetLifetime(manager, app, OpenIddictConstants.Settings.TokenLifetimes.AccessToken);
        }

        public async ValueTask<int> GetIdentityTokenLifetime(SpaceApplication app)
        {
            return await GetLifetime(manager, app, OpenIddictConstants.Settings.TokenLifetimes.IdentityToken);
        }

        public async ValueTask<int> GetRefreshTokenLifetime(SpaceApplication app)
        {
            return await GetLifetime(manager, app, OpenIddictConstants.Settings.TokenLifetimes.RefreshToken);
        }
    }

    private static async ValueTask<int> GetLifetime(OpenIddictApplicationManager<SpaceApplication> manager,
        SpaceApplication app,
        string settingName)
    {
        var settings = await manager.GetSettingsAsync(app);
        return int.Parse(settings.GetValueOrDefault(settingName, "0"));
    }
}
