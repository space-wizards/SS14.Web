using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace SS14.Web.Extensions;

public static class OpenIdExtension
{
    public static void AddOpenIdConnect(this WebApplicationBuilder builder)
    {
        var keyPath = builder.Configuration.GetValue<string>("Is4SigningKeyPath");
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

        var keyPathRsa = builder.Configuration.GetValue<string>("Is4SigningKeyPathRsa");
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

    public static void UseOpenIdConnect(this WebApplication app)
    {

    }
}
