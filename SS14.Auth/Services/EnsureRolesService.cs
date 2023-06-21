using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SS14.Auth.Shared;
using SS14.Auth.Shared.Data;

namespace SS14.Auth.Services;

/// <summary>
/// Ensures auth roles are available in the database.
/// </summary>
public sealed class EnsureRolesService : IHostedService
{
    private static readonly string[] RolesToEnsure = {
        AuthConstants.RoleSysAdmin,
        AuthConstants.RoleServerHubAdmin
    };

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EnsureRolesService> _logger;

    public EnsureRolesService(IServiceProvider serviceProvider, ILogger<EnsureRolesService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<SpaceRole>>();

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        foreach (var roleName in RolesToEnsure)
        {
            if (await roleManager.FindByNameAsync(roleName) != null)
                continue;
            
            _logger.LogInformation("Creating role {Role} because it does not exist in the database yet", roleName);

            await roleManager.CreateAsync(new SpaceRole
            {
                Name = roleName
            });
        }

        await tx.CommitAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}