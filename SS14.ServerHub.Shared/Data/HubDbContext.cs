using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace SS14.ServerHub.Shared.Data;

public sealed class HubDbContext : DbContext
{
    public HubDbContext(DbContextOptions<HubDbContext> options) : base(options)
    {
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

        modelBuilder.Entity<UniqueServerName>()
            .HasKey(e => new { e.AdvertisedServerId, e.Name });

        modelBuilder.Entity<HubAudit>()
            .HasIndex(e => e.Time);
    }

    public DbSet<AdvertisedServer> AdvertisedServer { get; set; } = default!;
    public DbSet<TrackedCommunity> TrackedCommunity { get; set; } = default!;
    public DbSet<TrackedCommunityAddress> TrackedCommunityAddress { get; set; } = default!;
    public DbSet<TrackedCommunityDomain> TrackedCommunityDomain { get; set; } = default!;
    public DbSet<TrackedCommunityInfoMatch> TrackedCommunityInfoMatch { get; set; } = default!;
    public DbSet<ServerStatusArchive> ServerStatusArchive { get; set; } = default!;
    public DbSet<UniqueServerName> UniqueServerName { get; set; } = default!;
    public DbSet<HubAudit> HubAudit { get; set; } = default!;
}
