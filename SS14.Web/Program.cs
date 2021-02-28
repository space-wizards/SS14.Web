using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using SS14.Auth.Shared;

[assembly: InternalsVisibleTo("SS14.Web.Tests")]

namespace SS14.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    var env = context.HostingEnvironment;
                    builder.AddYamlFile("appsettings.yml", false, true);
                    builder.AddYamlFile($"appsettings.{env.EnvironmentName}.yml", true, true);
                    builder.AddYamlFile("appsettings.Secret.yml", true, true);
                })
                .UseSerilog((ctx, cfg) =>
                {
                    cfg.ReadFrom.Configuration(ctx.Configuration);

                    StartupHelpers.SetupLoki(cfg, ctx.Configuration, "SS14.Web");
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    var webRoot = webBuilder.GetSetting("WEBROOT");
                    if (!string.IsNullOrEmpty(webRoot))
                    {
                        webBuilder.UseWebRoot(webRoot);
                    }

                    webBuilder.UseStartup<Startup>();
                })
                .UseSystemd();
        }
    }
}