using Microsoft.EntityFrameworkCore;

namespace SS14.ServerHub.Data
{
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
        }

        public DbSet<AdvertisedServer> AdvertisedServer { get; set; } = default!;
    }
}