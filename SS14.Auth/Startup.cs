using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;
using Quartz;
using SS14.Auth.Jobs;
using SS14.Auth.Services;
using SS14.Auth.Shared;
using SS14.Auth.Shared.Auth;
using SS14.WebEverythingShared;
using System;
using System.Threading.RateLimiting;

namespace SS14.Auth;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AuthHub", new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes("AuthHub")
                .Build());
        });

        services.AddRateLimiter(options =>
        {
            options.AddPolicy("registration", httpContext =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(15),
                        QueueLimit = 0,
                    });
            });

            options.AddPolicy("resend-confirmation", httpContext =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 2,
                        Window = TimeSpan.FromHours(1),
                        QueueLimit = 0,
                    });
            });

            options.AddPolicy("authenticate", httpContext => {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });

            options.AddPolicy("reset-password", httpContext => {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(15),
                        QueueLimit = 0
                    });
            });

            options.OnRejected = async (ctx, token) =>
            {
                ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await ctx.HttpContext.Response.WriteAsJsonAsync(new { Error = "rate_limited" }, token);
            };
        });

        services.AddAuthentication()
            .AddScheme<SS14AuthOptions, SS14AuthHandler>("SS14Auth", _ => {});

        services.AddHostedService<EnsureRolesService>();
        
        StartupHelpers.AddShared(services, Configuration);

        services.AddQuartz(q =>
        {
            if (Configuration.GetValue<bool>("IsPrimary"))
            {
                q.ScheduleJob<CleanOldSessionsJob>(trigger => trigger.WithSimpleSchedule(schedule =>
                {
                    schedule.RepeatForever().WithIntervalInHours(24);
                }));

                q.ScheduleJob<CleanOldAuthHashesJob>(trigger => trigger.WithSimpleSchedule(schedule =>
                {
                    schedule.RepeatForever().WithIntervalInHours(24);
                }));

                q.ScheduleJob<CleanOldAccountLogsJob>(trigger => trigger.WithSimpleSchedule(schedule =>
                {
                    schedule.RepeatForever().WithIntervalInHours(24);
                }));

                q.ScheduleJob<DeleteUnconfirmedAccounts>(trigger => trigger.WithSimpleSchedule(schedule =>
                {
                    schedule.RepeatForever().WithIntervalInHours(24);
                }));
            }
        });
        services.AddQuartzHostedService();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // app.UseHttpsRedirection();

        MoreStartupHelpers.AddForwardedSupport(app, Configuration);

        app.UseRouting();

        app.UseRateLimiter();

        app.UseHttpMetrics();
        
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapMetrics();
        });
    }
}
