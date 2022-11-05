using System;
using System.Threading.Tasks;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Extensions;
using IdentityServer4.EntityFramework.Interfaces;
using IdentityServer4.EntityFramework.Options;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SS14.Auth.Shared.Data;

public class ApplicationDbContext : IdentityDbContext<SpaceUser, SpaceRole, Guid>,
    IDataProtectionKeyContext,
    IConfigurationDbContext,
    IPersistedGrantDbContext
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

        builder.Entity<BurnerEmail>()
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

        var cfgStoreOptions = new ConfigurationStoreOptions
        {
            IdentityResource = new TableConfiguration("IdentityResources", "IS4"),
            IdentityResourceClaim = new TableConfiguration("IdentityResourceClaims", "IS4"),
            IdentityResourceProperty = new TableConfiguration("IdentityResourceProperties", "IS4"),
            ApiResource = new TableConfiguration("ApiResources", "IS4"),
            ApiResourceSecret = new TableConfiguration("ApiResourceSecrets", "IS4"),
            ApiResourceScope = new TableConfiguration("ApiResourceScopes", "IS4"),
            ApiResourceClaim = new TableConfiguration("ApiResourceClaims", "IS4"),
            ApiResourceProperty = new TableConfiguration("ApiResourceProperties", "IS4"),
            Client = new TableConfiguration("Clients", "IS4"),
            ClientGrantType = new TableConfiguration("ClientGrantTypes", "IS4"),
            ClientRedirectUri = new TableConfiguration("ClientRedirectUris", "IS4"),
            ClientPostLogoutRedirectUri = new TableConfiguration("ClientPostLogoutRedirectUris", "IS4"),
            ClientScopes = new TableConfiguration("ClientScopes", "IS4"),
            ClientSecret = new TableConfiguration("ClientSecrets", "IS4"),
            ClientClaim = new TableConfiguration("ClientClaims", "IS4"),
            ClientIdPRestriction = new TableConfiguration("ClientIdPRestrictions", "IS4"),
            ClientCorsOrigin = new TableConfiguration("ClientCorsOrigins", "IS4"),
            ClientProperty = new TableConfiguration("ClientProperties", "IS4"),
            ApiScope = new TableConfiguration("ApiScopes", "IS4"),
            ApiScopeClaim = new TableConfiguration("ApiScopeClaims", "IS4"),
            ApiScopeProperty = new TableConfiguration("ApiScopeProperties", "IS4")
        };
        builder.ConfigureClientContext(cfgStoreOptions);
        builder.ConfigureResourcesContext(cfgStoreOptions);
        builder.ConfigurePersistedGrantContext(new OperationalStoreOptions
        {
            PersistedGrants = new TableConfiguration("PersistedGrants", "IS4"),
            DeviceFlowCodes = new TableConfiguration("DeviceCodes", "IS4"),
        });
    }

    public DbSet<LoginSession> ActiveSessions { get; set; }
    public DbSet<AuthHash> AuthHashes { get; set; }
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
    public DbSet<BurnerEmail> BurnerEmails { get; set; }
    public DbSet<Patron> Patrons { get; set; }
    public DbSet<PatreonWebhookLog> PatreonWebhookLogs { get; set; }
    public DbSet<UserOAuthClient> UserOAuthClients { get; set; }
    public DbSet<PastAccountName> PastAccountNames { get; set; }
    public DbSet<AccountLog> AccountLogs { get; set; }

    // IS4 configuration.
    public DbSet<Client> Clients { get; set; }
    public DbSet<ClientSecret> ClientSecrets { get; set; }
    public DbSet<ClientCorsOrigin> ClientCorsOrigins { get; set; }
    public DbSet<IdentityResource> IdentityResources { get; set; }
    public DbSet<ApiResource> ApiResources { get; set; }
    public DbSet<ApiScope> ApiScopes { get; set; }

    // IS4 operational.
    public DbSet<PersistedGrant> PersistedGrants { get; set; }
    public DbSet<DeviceFlowCodes> DeviceFlowCodes { get; set; }

    Task<int> IPersistedGrantDbContext.SaveChangesAsync()
    {
        return base.SaveChangesAsync();
    }
}