using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SS14.ServerHub.Shared.Data;

[UsedImplicitly]
public sealed class HubDesignTimeDbContextFactory : IDesignTimeDbContextFactory<HubDbContext>
{
    public HubDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HubDbContext>();
        optionsBuilder.UseNpgsql("Server=localhost");
        return new HubDbContext(optionsBuilder.Options);
    }
}
