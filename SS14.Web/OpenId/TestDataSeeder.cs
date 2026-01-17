using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using SS14.Auth.Shared;
using SS14.Auth.Shared.Data;

namespace SS14.Web.OpenId;

// TODO: Remove after development
public sealed class TestDataSeeder(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        await PopulateInternalApps(scope, ct);
        await PopulateUsersAsync(scope);
    }

    public Task StopAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    private async ValueTask PopulateInternalApps(IServiceScope scope, CancellationToken ct)
    {
        var appManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var appDescriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = "test_client",
            ClientSecret = "test_secret",
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            DisplayName = "Test Client",
            RedirectUris = { new Uri("https://localhost:5002/signin-oidc") },
            Requirements = { OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange },
            Settings =
            {
                {OpenIdConstants.SigningAlgorithmSetting, "RS256"},
                {OpenIdConstants.AllowPlainPkce, "true"},
            },
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.Introspection,
                OpenIddictConstants.Permissions.Endpoints.EndSession,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Roles,
            }
        };

        var client = await appManager.FindByClientIdAsync(appDescriptor.ClientId, ct);
        if (client == null)
        {
            await appManager.CreateAsync(appDescriptor, ct);
        }
        else
        {
            await appManager.UpdateAsync(client, appDescriptor, ct);
        }
    }

    private async ValueTask PopulateUsersAsync(IServiceScope scope)
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<SpaceUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<SpaceRole>>();

        var sysAdmin = new SpaceRole
        {
            Name = AuthConstants.RoleSysAdmin,
        };

        await roleManager.CreateAsync(sysAdmin);

        var hubAdmin = new SpaceRole
        {
            Name = AuthConstants.RoleServerHubAdmin,
        };

        await roleManager.CreateAsync(hubAdmin);

        var user = new SpaceUser
        {
            UserName = "TestUser",
            Email = "test@example.test",
            EmailConfirmed = true,
        };

        await userManager.CreateAsync(user, "Test123456$");
        await userManager.AddToRoleAsync(user, AuthConstants.RoleSysAdmin);
        await userManager.AddToRoleAsync(user, AuthConstants.RoleServerHubAdmin);
    }
}
