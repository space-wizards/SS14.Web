using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SS14.ServerHub.Shared.Data;

namespace SS14.Web.Extensions;

public static class OpenIdExtension
{
    public static void AddOpenIdConnect(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenIddict()
            .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<HubDbContext>())
            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris("connect/authorize", "connect/authorize/accept", "connect/authorize/deny")
                    .SetTokenEndpointUris("connect/token")
                    .SetEndSessionEndpointUris("connect/endsession")
                    .SetUserInfoEndpointUris("connect/userinfo")
                    .SetIntrospectionEndpointUris("connect/introspect");

                options.AllowAuthorizationCodeFlow();

                options.ConfigureKeys(builder);

            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });
    }

    public static void UseOpenIdConnect(this WebApplication app)
    {

    }

    private static void ConfigureKeys(this OpenIddictServerBuilder options,  WebApplicationBuilder builder)
    {
        var deprecatedKeyPath = builder.Configuration.GetValue<string>("Is4SigningKeyPath");
        if (deprecatedKeyPath != null)
            Log.Warning("Using deprecated Is4SigningKeyPath. Use OidcSigningKeyPath instead.");

        var keyPath = deprecatedKeyPath ?? builder.Configuration.GetValue<string>("OidcSigningKeyPath");

        if (keyPath == null)
        {
            if (builder.Environment.IsDevelopment())
            {
                Log.Debug("Using developer signing credentials");
                //builder.AddDeveloperSigningCredential();
            }
            else
            {
                throw new Exception("No key specified for IS4!");
            }
        }
        else
        {
            var keyPem = File.ReadAllText(keyPath);
            var key = ECDsa.Create();
            key.ImportFromPem(keyPem);

            //builder.AddSigningCredential(
            //    new ECDsaSecurityKey(key),
            //    IdentityServerConstants.ECDsaSigningAlgorithm.ES256);
        }

        var deprecatedKeyPathRsa = builder.Configuration.GetValue<string>("Is4SigningKeyPathRsa");
        if (deprecatedKeyPathRsa != null)
            Log.Warning("Using deprecated Is4SigningKeyPathRsa. Use OidcSigningKeyPathRsa instead.");

        var keyPathRsa = deprecatedKeyPathRsa ?? builder.Configuration.GetValue<string>("OidcSigningKeyPathRsa");

        if (keyPathRsa != null)
        {
            var keyPem = File.ReadAllText(keyPathRsa);
            var key = RSA.Create();
            key.ImportFromPem(keyPem);

            //builder.AddSigningCredential(
            //    new RsaSecurityKey(key),
            //    IdentityServerConstants.RsaSigningAlgorithm.PS256);
            //builder.AddSigningCredential(
            //    new RsaSecurityKey(key),
            //    IdentityServerConstants.RsaSigningAlgorithm.RS256);
        }

    }
}
