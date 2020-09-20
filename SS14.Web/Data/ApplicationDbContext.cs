using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SS14.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<SpaceUser, SpaceRole, Guid>
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (_initializedWithoutOptions)
            {
                optionsBuilder.UseNpgsql("foobar");
            }
        }

        private readonly bool _initializedWithoutOptions;

        public ApplicationDbContext()
        {
            _initializedWithoutOptions = true;
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<SpaceUser>()
                .Ignore(p => p.PhoneNumber)
                .Ignore(p => p.PhoneNumberConfirmed)
                // 2FA disabled for now because I'd have to modify the launcher.
                .Ignore(p => p.TwoFactorEnabled)
                // I don't (currently) care about any of this lockout stuff.
                .Ignore(p => p.LockoutEnd)
                .Ignore(p => p.LockoutEnabled)
                .Ignore(p => p.AccessFailedCount);

            builder.Entity<LoginSession>()
                .HasIndex(p => p.Token)
                .IsUnique();

            builder.Entity<AuthHash>()
                .HasIndex(p => new {p.Hash, p.SpaceUserId})
                .IsUnique();
        }

        public DbSet<LoginSession> ActiveSessions { get; set; }
        public DbSet<AuthHash> AuthHashes { get; set; }
    }
}