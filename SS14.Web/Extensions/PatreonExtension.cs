using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SS14.Auth.Shared.Config;

namespace SS14.Web.Extensions;

public static class PatreonExtension
{
    public static void AddPatreon(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<PatreonConnectionHandler>();
        var patreonSection = builder.Configuration.GetSection("Patreon");
        var patreonCfg = patreonSection.Get<PatreonConfiguration>();

        if (patreonCfg?.ClientId == null || patreonCfg.ClientSecret == null)
            return;

        builder.Services.AddAuthentication()
            // Rider is dumb that null is valid.
            // It disables Patreon as an external login.
            .AddPatreon("Patreon", null!, options =>
            {
                // Patreon docs lied you don't need this to see memberships to your own campaign.
                // options.Scope.Add("identity.memberships");
                options.Includes.Add("memberships.currently_entitled_tiers");
                options.ClientId = patreonCfg.ClientId;
                options.ClientSecret = patreonCfg.ClientSecret;

                options.Events.OnCreatingTicket += context =>
                {
                    var handler = context.HttpContext.RequestServices.GetService<PatreonConnectionHandler>();
                    return handler!.HookCreatingTicket(context);
                };

                options.Events.OnTicketReceived += context =>
                {
                    var handler = context.HttpContext.RequestServices.GetService<PatreonConnectionHandler>();
                    return handler!.HookReceivedTicket(context);
                };
            });
    }
}
