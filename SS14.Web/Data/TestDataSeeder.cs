using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;

namespace SS14.Web.Data;

// TODO: Remove after development
public sealed class TestDataSeeder(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        await PopulateInternalApps(scope, ct);
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
            RedirectUris = { new Uri("https://localhost:7175/callback") },
            Requirements = { OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange },
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
                OpenIddictConstants.Permissions.Scopes.Roles
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
}
