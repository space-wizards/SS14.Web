#nullable enable
using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;
using Serilog;
using SS14.Auth.Shared;
using SS14.Auth.Shared.Data;
using SS14.ServerHub.Shared.Data;
using SS14.Web;
using SS14.Web.Data;
using SS14.Web.Extensions;
using SS14.Web.HCaptcha;
using SS14.WebEverythingShared;
using static SS14.Auth.Shared.Data.OpeniddictDefaultTypes;

var builder = WebApplication.CreateBuilder();

// Configuration
var env = builder.Environment;
builder.Configuration.AddYamlFile("appsettings.yml", false, true);
builder.Configuration.AddYamlFile($"appsettings.{env.EnvironmentName}.yml", true, true);
builder.Configuration.AddYamlFile("appsettings.Secret.yml", true, true);

builder.Services.Configure<AccountOptions>(builder.Configuration.GetSection("Account"));

// Logging
builder.Services.AddSerilog(config =>
{
    config.ReadFrom.Configuration(builder.Configuration);
    StartupHelpers.SetupLoki(config, builder.Configuration, "SS14.Web");
});

builder.Services.AddScoped<HubAuditLogManager>();

// Database

builder.Services.AddDbContext<HubDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("HubConnection")
        ?? throw new InvalidOperationException("Must set HubConnection");

    options.UseNpgsql(connectionString);
});

// Auth

builder.Services.AddAuthorizationBuilder()
   .AddPolicy(AuthConstants.PolicyAnyHubAdmin, policy => policy.RequireRole(AuthConstants.RoleSysAdmin, AuthConstants.RoleServerHubAdmin) )
   .AddPolicy(AuthConstants.PolicySysAdmin, policy => policy.RequireRole(AuthConstants.RoleSysAdmin) )
   .AddPolicy(AuthConstants.PolicyServerHubAdmin, policy => policy.RequireRole(AuthConstants.RoleServerHubAdmin) );

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = $"/Identity/Account/Login";
    options.LogoutPath = $"/Identity/Account/Logout";
    options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
});

// MVC

builder.Services.AddMvc().AddRazorPagesOptions(options =>
{
    options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
    options.Conventions.AuthorizeAreaPage("Identity", "/Account/Logout");
    options.Conventions.AuthorizeAreaFolder("Admin", "/", AuthConstants.PolicyAnyHubAdmin);
    options.Conventions.AuthorizeAreaFolder("Admin", "/Clients", AuthConstants.PolicySysAdmin);
    options.Conventions.AuthorizeAreaFolder("Admin", "/Users", AuthConstants.PolicySysAdmin);
    options.Conventions.AuthorizeAreaFolder("Admin", "/Servers", AuthConstants.PolicyServerHubAdmin);
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Services
builder.Services.AddSystemd();
HCaptchaService.RegisterServices(builder.Services, builder.Configuration);
builder.AddPatreon();
builder.AddOpenIdConnect();
builder.Services.AddScoped<PersonalDataCollector>();
builder.AddShared();

builder.Services.Configure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme,
    options =>
    {
        options.LoginPath = $"/Identity/Account/Login";
        options.LogoutPath = $"/Identity/Account/Logout";
        options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
    });

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms to {ClientAddress}";
    options.EnrichDiagnosticContext = (context, httpContext) =>
    {
        if (httpContext.Connection.RemoteIpAddress != null)
            context.Set("ClientAddress", httpContext.Connection.RemoteIpAddress);
    };
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

MoreStartupHelpers.AddForwardedSupport(app, app.Configuration);

var pathBase = app.Configuration.GetValue<string>("PathBase");
if (!string.IsNullOrEmpty(pathBase))
    app.UsePathBase(pathBase);

app.UseStaticFiles();

app.UseRouting();

app.UseHttpMetrics();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultControllerRoute();
app.MapRazorPages();
app.MapMetrics();

app.UseOpenIdConnect();

app.Run();
