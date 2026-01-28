#nullable enable
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using SS14.Auth.Shared.Data;
using SS14.Web.OpenId.Configuration;
using SS14.Web.OpenId.EventHandlers;
using SS14.Web.OpenId.Services;
using static SS14.Auth.Shared.Data.OpeniddictDefaultTypes;

namespace SS14.Web.OpenId.Extensions;

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
            options.UseQuartz();
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

    private static void ConfigureCertificates(OpenIddictBuilder openId, WebApplicationBuilder builder)
    {
        builder.Services.Add(AuthorizationPkceVerificationHandler.Descriptor.ServiceDescriptor);
        openId.AddServer().AddEventHandler(AuthorizationPkceVerificationHandler.Descriptor);

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

        foreach (var signingCertificate in config.SigningCertificates)
        {
            using var signingCert = File.OpenRead(signingCertificate.Path);

            if (signingCertificate.Algorithm == null)
            {
                openId.AddServer().AddSigningCertificate(signingCert, signingCertificate.Password);
            }
            else
            {
                using var buffer = new MemoryStream();
                signingCert.CopyTo(buffer);
                var cert = X509CertificateLoader.LoadPkcs12(
                    buffer.ToArray(),
                    signingCertificate.Password,
                    X509KeyStorageFlags.EphemeralKeySet);

                var key = new X509SecurityKey(cert);
                var credentials = new SigningCredentials(key, signingCertificate.Algorithm);
                openId.AddServer().AddSigningCredentials(credentials);
            }
        }
    }

    private static void AddCertificate(OpenIddictBuilder openId, FileStream cert, string password)
    {

    }

    private static void AddCertificate(OpenIddictBuilder openId, FileStream cert, string password, string algorithm)
    {

    }
}
