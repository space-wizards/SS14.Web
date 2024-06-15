using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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

        app.UseHttpMetrics();
        
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapMetrics();
        });
    }
}