using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SS14.Auth.Shared;
using SS14.Auth.Shared.Config;
using SS14.Auth.Shared.Data;

namespace SS14.Web
{
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
            services.AddDatabaseDeveloperPageExceptionFilter();
            StartupHelpers.AddShared(services, Configuration);

            services.AddMvc()
                .AddRazorPagesOptions(options =>
                {
                    options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
                    options.Conventions.AuthorizeAreaPage("Identity", "/Account/Logout");
                });

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = $"/Identity/Account/Login";
                options.LogoutPath = $"/Identity/Account/Logout";
                options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
            });

            services.AddControllersWithViews();
            services.AddRazorPages();

            services.AddScoped<PatreonConnectionHandler>();

            var patreonSection = Configuration.GetSection("Patreon");
            var patreonCfg = patreonSection.Get<PatreonConfiguration>();

            services.AddAuthentication(options => options.DefaultScheme = IdentityConstants.ApplicationScheme);

            if (patreonCfg?.ClientId != null && patreonCfg.ClientSecret != null)
            {
                services.AddAuthentication()
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

            services.AddIdentityServer()
                .AddAspNetIdentity<SpaceUser>()
                .AddDeveloperSigningCredential()
                .AddOperationalStore<ApplicationDbContext>()
                .AddConfigurationStore<ApplicationDbContext>()
                /*.AddInMemoryClients(new[]
                {
                    new Client
                    {
                        ClientId = "A",
                        ClientSecrets = {new Secret("A".Sha256())},
                        AllowedGrantTypes = GrantTypes.Code,
                        // RequireConsent = true,
                        RedirectUris = {"https://localhost:5002/signin-oidc"},
                        AllowedScopes =
                        {
                            IdentityServerConstants.StandardScopes.Profile,
                            IdentityServerConstants.StandardScopes.OpenId,
                            IdentityServerConstants.StandardScopes.Email,
                            "A",
                        },

                        // AllowOfflineAccess = true,
                    },
                    new Client
                    {
                        ClientId = "invision",
                        ClientSecrets = {new Secret("foobar".Sha256())},
                        AllowedGrantTypes = GrantTypes.Code,
                        // RequireConsent = true,
                        RedirectUris =
                        {
                            "https://t309923.invisionservice.com/oauth/callback/",
                            "https://mommibb.spacestation14.io/entry/oauth2"
                        },
                        AllowedScopes =
                        {
                            IdentityServerConstants.StandardScopes.Profile,
                            IdentityServerConstants.StandardScopes.OpenId,
                            IdentityServerConstants.StandardScopes.Email,
                            "A"
                        },
                        RequirePkce = false

                        // AllowOfflineAccess = true,
                    }
                })
                .AddInMemoryIdentityResources(new List<IdentityResource>
                {
                    new IdentityResources.OpenId(),
                    new IdentityResources.Profile(),
                    new IdentityResources.Email(),
                })
                .AddInMemoryApiScopes(new[] {new ApiScope("A")})
                .AddInMemoryApiResources(Array.Empty<ApiResource>())*/;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSerilogRequestLogging();

            if (env.IsDevelopment())
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

            var forwardedHeadersOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            };

            foreach (var ip in Configuration.GetSection("ForwardProxies").Get<string[]>())
            {
                forwardedHeadersOptions.KnownProxies.Add(IPAddress.Parse(ip));
            }

            app.UseForwardedHeaders(forwardedHeadersOptions);

            var pathBase = Configuration.GetValue<string>("PathBase");
            if (!string.IsNullOrEmpty(pathBase))
            {
                app.UsePathBase(pathBase);
            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });

            app.UseIdentityServer();
        }
    }
}