#nullable enable
using System.Threading.Tasks;
using OpenIddict.Core;
using SS14.Auth.Shared.Data;

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

    public static async ValueTask<string?> GetHomePageProperty(
        this OpenIddictApplicationManager<SpaceApplication> manager,
        SpaceApplication app)
    {
        var properties = await manager.GetPropertiesAsync(app);
        return properties.TryGetValue("homepage", out var homepage) ? homepage.GetString() : null;
    }

    public static async ValueTask<bool> GetRequiresPkceSetting(
        this OpenIddictApplicationManager<SpaceApplication> manager,
        SpaceApplication app)
    {
        var settings = await manager.GetSettingsAsync(app);
        return settings.ContainsKey("ft:pkce");
    }
}
