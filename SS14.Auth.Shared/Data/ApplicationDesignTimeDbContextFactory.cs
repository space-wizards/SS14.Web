using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using static SS14.Auth.Shared.Data.OpeniddictDefaultTypes;

namespace SS14.Auth.Shared.Data;

[UsedImplicitly]
public sealed class ApplicationDesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql("Server=localhost");
        optionsBuilder.UseOpenIddict<SpaceApplication, DefaultAuthorization, DefaultScope, DefaultToken, string>();
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
