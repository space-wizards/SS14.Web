using System;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace OAuthTest;

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
        var settings = Configuration.GetSection("OAuthTest").Get<OAuthTestSettings>()
                       ?? throw new InvalidOperationException("Missing OAuthTest config section");

        services.AddHttpLogging(logging => { logging.LoggingFields = settings.HttpLoggingFields; });

        if (!settings.MapInboundClaims)
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        services.AddControllersWithViews();

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "oidc";
            })
            .AddCookie("Cookies")
            .AddOpenIdConnect("oidc", options =>
            {
                options.SignInScheme = "Cookies";

                options.Authority = settings.Authority;
                options.ClientId = settings.ClientId;
                options.ClientSecret = settings.ClientSecret;

                options.GetClaimsFromUserInfoEndpoint = settings.GetClaimsFromUserInfoEndpoint;
                options.ResponseType = settings.ResponseType;
                options.SaveTokens = settings.SaveTokens;
                options.UsePkce = settings.UsePkce;
                options.RequireHttpsMetadata = settings.RequireHttpsMetadata;
                options.MapInboundClaims = settings.MapInboundClaims;

                foreach (var scope in settings.Scopes)
                {
                    options.Scope.Add(scope);
                }
            });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpLogging();
        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        });
    }
}

/// <summary>
/// Bound from the "OAuthTest" section of appsettings
/// </summary>
public sealed class OAuthTestSettings
{
    public string Authority { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string ResponseType { get; set; } = "code";
    public bool UsePkce { get; set; } = true;
    public bool GetClaimsFromUserInfoEndpoint { get; set; } = true;
    public bool SaveTokens { get; set; }
    public bool RequireHttpsMetadata { get; set; } = true;
    public bool MapInboundClaims { get; set; }
    public HttpLoggingFields HttpLoggingFields { get; set; } = HttpLoggingFields.All;
    public string[] Scopes { get; set; } = Array.Empty<string>();
}
