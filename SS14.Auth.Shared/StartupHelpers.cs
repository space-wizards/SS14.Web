using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Internal;
using SS14.Auth.Shared.Data;
using SS14.Auth.Shared.Emails;
using SS14.Auth.Shared.Sessions;

namespace SS14.Auth.Shared
{
    public static class StartupHelpers
    {
        public static void AddShared(IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(
                    config.GetConnectionString("DefaultConnection")));

            services.AddDataProtection()
                .PersistKeysToDbContext<ApplicationDbContext>()
                .SetApplicationName("SS14.Auth.Shared");

            services.AddSingleton<IUserValidator<SpaceUser>, SS14UserValidator>();
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
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            if (string.IsNullOrEmpty(config.GetValue<string>("Email:Host")))
            {
                // Dummy emails.
                services.AddSingleton<IEmailSender, DummyEmailSender>();
            }
            else
            {
                services.Configure<SmtpEmailOptions>(config.GetSection("Email"));
                services.AddSingleton<IEmailSender, SmtpEmailSender>();
            }

            services.AddSingleton<IEmailSender, SmtpEmailSender>();
            services.AddScoped<SessionManager>();

            services.AddTransient(_ => RandomNumberGenerator.Create());

            services.TryAddSingleton<ISystemClock, SystemClock>();
        }
    }
}