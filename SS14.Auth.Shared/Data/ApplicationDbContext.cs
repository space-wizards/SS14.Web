using System;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SS14.Auth.Shared.Data;

public class ApplicationDbContext : IdentityDbContext<SpaceUser, SpaceRole, Guid>,
    IDataProtectionKeyContext
{
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
            .Ignore(p => p.LockoutEnd)
            .Ignore(p => p.LockoutEnabled)
            .Ignore(p => p.AccessFailedCount);

        builder.Entity<LoginSession>()
            .HasIndex(p => p.Token)
            .IsUnique();

        builder.Entity<AuthHash>()
            .HasIndex(p => new {p.Hash, p.SpaceUserId})
            .IsUnique();

        builder.Entity<AuthHash>()
            .HasOne(h => h.Hwid)
            .WithMany()
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<BurnerEmail>()
            .HasIndex(p => new {p.Domain})
            .IsUnique();

        builder.Entity<WhitelistEmail>()
            .HasIndex(p => new {p.Domain})
            .IsUnique();

        builder.Entity<Patron>()
            .HasIndex(p => p.PatreonId)
            .IsUnique();

        builder.Entity<Patron>()
            .HasIndex(p => p.SpaceUserId)
            .IsUnique();

        builder.Entity<UserOAuthClient>()
            .HasIndex(p => new { p.ClientId })
            .IsUnique();

        builder.Entity<Hwid>()
            .HasIndex(h => h.ClientData)
            .IsUnique();

        builder.Entity<HwidUser>()
            .HasIndex(h => new { h.HwidId, h.SpaceUserId })
            .IsUnique();
    }

    public DbSet<LoginSession> ActiveSessions { get; set; }
    public DbSet<AuthHash> AuthHashes { get; set; }
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
    public DbSet<BurnerEmail> BurnerEmails { get; set; }
    public DbSet<WhitelistEmail> WhitelistEmails { get; set; }
    public DbSet<Patron> Patrons { get; set; }
    public DbSet<PatreonWebhookLog> PatreonWebhookLogs { get; set; }
    public DbSet<UserOAuthClient> UserOAuthClients { get; set; }
    public DbSet<PastAccountName> PastAccountNames { get; set; }
    public DbSet<AccountLog> AccountLogs { get; set; }
    public DbSet<Hwid> Hwids { get; set; }
    public DbSet<HwidUser> HwidUsers { get; set; }
}
