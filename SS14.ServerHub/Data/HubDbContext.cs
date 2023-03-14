using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace SS14.ServerHub.Data;

public sealed class HubDbContext : DbContext
{
    public HubDbContext(DbContextOptions<HubDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
            
        optionsBuilder.ReplaceService<IRelationalTypeMappingSource, CustomNpgsqlTypeMappingSource>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Address must be valid ss14:// or ss14s:// URI.
        modelBuilder.Entity<AdvertisedServer>()
            .HasCheckConstraint("AddressSs14Uri", "\"Address\" LIKE 'ss14://%' OR \"Address\" LIKE 'ss14s://%'");

        // Unique index on address.
        modelBuilder.Entity<AdvertisedServer>()
            .HasIndex(e => e.Address)
            .IsUnique();

        modelBuilder.Entity<ServerStatusArchive>()
            .HasKey(e => new { e.AdvertisedServerId, e.ServerStatusArchiveId });

        modelBuilder.Entity<ServerStatusArchive>()
            .Property(e => e.ServerStatusArchiveId)
            .ValueGeneratedOnAdd();
    }

    public DbSet<AdvertisedServer> AdvertisedServer { get; set; } = default!;
    public DbSet<BannedAddress> BannedAddress { get; set; } = default!;
    public DbSet<BannedDomain> BannedDomain { get; set; } = default!;
    public DbSet<ServerStatusArchive> ServerStatusArchive { get; set; } = default!;
}