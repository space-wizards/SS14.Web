using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Internal;
using Serilog;
using Serilog.Sinks.Loki;
using Serilog.Sinks.Loki.Labels;
using SS14.Auth.Shared.Config;
using SS14.Auth.Shared.Data;
using SS14.Auth.Shared.Emails;
using SS14.Auth.Shared.MutexDb;
using SS14.Auth.Shared.Sessions;

namespace SS14.Auth.Shared;

public static class StartupHelpers
{
    public static void AddShared(IServiceCollection services, IConfiguration config)
    {
        // Configure.
        services.Configure<LimitOptions>(config.GetSection("Limits"));
        services.Configure<MutexOptions>(config.GetSection("Mutex"));
        services.Configure<PatreonConfiguration>(config.GetSection("Patreon"));
        services.Configure<DiscordConfiguration>(config.GetSection("Discord"));
        services.Configure<SecurityStampValidatorOptions>(options =>
        {
            // The fact that this isn't default absolutely baffles me.
            options.ValidationInterval = TimeSpan.FromSeconds(5);
        });
            
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                config.GetConnectionString("DefaultConnection")));

        services.AddDataProtection()
            .PersistKeysToDbContext<ApplicationDbContext>()
            .SetApplicationName("SS14.Auth.Shared");

        services.AddScoped<IUserValidator<SpaceUser>, SS14UserValidator>();
        services.AddScoped<UserManager<SpaceUser>, SpaceUserManager>();
        services.AddScoped<SpaceUserManager>();
        services.AddIdentity<SpaceUser, SpaceRole>(o =>
            {
                o.Password.RequireDigit = false;
                o.Password.RequireLowercase = false;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                o.SignIn.RequireConfirmedEmail = true;
                o.SignIn.RequireConfirmedAccount = true;
                o.User.RequireUniqueEmail = true;
                // We use our own username validation logic.
                // See SS14UserValidator.
                o.User.AllowedUserNameCharacters = null;
            })
            .AddRoles<SpaceRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddTransient<IEmailSender, EmailSender>();
        if (string.IsNullOrEmpty(config.GetValue<string>("Email:Host")))
        {
            // Dummy emails.
            services.AddSingleton<IRawEmailSender, DummyEmailSender>();
        }
        else
        {
            services.Configure<SmtpEmailOptions>(config.GetSection("Email"));
            services.AddSingleton<IRawEmailSender, SmtpEmailSender>();
        }

        services.AddScoped<SessionManager>();
        services.AddScoped<PatreonDataManager>();
        services.AddScoped<DiscordDataManager>();
        services.AddScoped<DiscordLoginSessionManager>();

        services.AddTransient(_ => RandomNumberGenerator.Create());

        services.TryAddSingleton<ISystemClock, SystemClock>();

        services.AddTransient<MutexDatabase>();
    }
        
    public static void SetupLoki(LoggerConfiguration log, IConfiguration cfg, string appName)
    {
        var dat = cfg.GetSection("Serilog:Loki").Get<LokiConfigurationData>();

        if (dat == null)
            return;

        LokiCredentials credentials;
        if (string.IsNullOrWhiteSpace(dat.Username))
        {
            credentials = new NoAuthCredentials(dat.Address);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dat.Password))
            {
                throw new InvalidDataException("No password specified.");
            }

            credentials = new BasicAuthCredentials(dat.Address, dat.Username, dat.Password);
        }

        log.WriteTo.LokiHttp(credentials, new DefaultLogLabelProvider(new[]
        {
            new LokiLabel("App", appName),
            new LokiLabel("Server", dat.Name)
        }));
    }

    private sealed class LokiConfigurationData
    {
        public string Address { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
