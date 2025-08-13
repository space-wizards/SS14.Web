#nullable enable
using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Client.AspNetCore;
using OpenIddict.Server;
using SS14.Auth.Shared.Data;
using SS14.ServerHub.Shared.Data;
using SS14.Web.Configuration;
using SS14.Web.Data;
using SS14.Web.Services;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static SS14.Auth.Shared.Data.OpeniddictDefaultTypes;

namespace SS14.Web.Extensions;
// TODO: Add integration with quartz
public static class OpenIdExtension
{
    public static void AddOpenIdConnect(this WebApplicationBuilder builder)
    {
        var openId = builder.Services.AddOpenIddict();
        openId.AddCore(options =>
        {
            options.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>()
                .ReplaceDefaultEntities<SpaceApplication, DefaultAuthorization, DefaultScope, DefaultToken, string>();
        });

        openId.AddValidation().UseLocalServer();
        openId.AddValidation().UseAspNetCore();

        openId.AddServer()
            .Configure(config => builder.Configuration.Bind("OpenId:Server", config))
            .UseAspNetCore().EnableAuthorizationEndpointPassthrough().EnableStatusCodePagesIntegration();
        ConfigureCertificates(openId, builder);

        builder.Services.AddHostedService<TestDataSeeder>();
        builder.Services.AddScoped<IdentityClaimsProvider>();
        builder.Services.AddScoped<SignedInIdentityService>();
        builder.Services.AddScoped<OpenIdActionService>();
    }

    public static void UseOpenIdConnect(this WebApplication app)
    {

    }

    private static void ConfigureCertificates(OpenIddictBuilder openId, WebApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment())
        {
            openId.AddServer().AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();
            return;
        }

        var config = builder.Configuration
            .GetSection("OpenId")
            .GetSection(OpenIdCertificateConfiguration.Name).Get<OpenIdCertificateConfiguration>();

        if (config?.EncryptionCertificatePath == null || config.SigningCertificatePath == null)
            throw new InvalidOperationException("Encryption and signing certificates not configured");

        using var encryptionCert = File.OpenRead(config.EncryptionCertificatePath);
        openId.AddServer().AddEncryptionCertificate(encryptionCert, config.EncryptionCertificatePassword);

        using var signingCert = File.OpenRead(config.SigningCertificatePath);
        openId.AddServer().AddSigningCertificate(signingCert, config.SigningCertificatePassword);
    }
}
