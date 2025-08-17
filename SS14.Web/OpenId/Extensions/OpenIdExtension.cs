#nullable enable
using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SS14.Auth.Shared.Data;
using SS14.Web.Data;
using SS14.Web.OpenId.Configuration;
using SS14.Web.OpenId.EventHandlers;
using SS14.Web.OpenId.Services;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static SS14.Auth.Shared.Data.OpeniddictDefaultTypes;

namespace SS14.Web.OpenId.Extensions;
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

            options.ReplaceApplicationManager<SpaceApplication, SpaceApplicationManager>();
        });

        openId.AddValidation().UseLocalServer();
        openId.AddValidation().UseAspNetCore();

        openId.AddServer().Configure(config => builder.Configuration.Bind("OpenId:Server", config));
        openId.AddServer().UseAspNetCore().EnableAuthorizationEndpointPassthrough().EnableStatusCodePagesIntegration();
        ConfigureCertificates(openId, builder);

        builder.Services.AddScoped<SpaceApplicationManager>();
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
        builder.Services.Add(TokenSigningHandler.Descriptor.ServiceDescriptor);
        openId.AddServer().AddEventHandler(TokenSigningHandler.Descriptor);

        if (builder.Environment.IsDevelopment())
        {
            openId.AddServer().AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();
            return;
        }

        var config = builder.Configuration
            .GetSection("OpenId")
            .GetSection(OpenIdCertificateConfiguration.Name).Get<OpenIdCertificateConfiguration>();

        if (config is null)
            throw new ArgumentException("OpenId:Server:CertificateConfiguration is not set.");

        foreach (var encryptionCertificate in config.EncryptionCertificates)
        {
            using var encryptionCert = File.OpenRead(encryptionCertificate.Path);
            openId.AddServer().AddEncryptionCertificate(encryptionCert, encryptionCertificate.Password);
        }

        foreach (var signingCertificate in config.EncryptionCertificates)
        {
            using var signingCert = File.OpenRead(signingCertificate.Path);;
            openId.AddServer().AddSigningCertificate(signingCert, signingCertificate.Password);
        }
    }
}
