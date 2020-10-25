using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

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
            var config = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    var env = context.HostingEnvironment;
                    builder.AddYamlFile("appsettings.yml", false, true);
                    builder.AddYamlFile("appsettings.Secret.yml", true, true);
                    builder.AddYamlFile($"appsettings.{env.EnvironmentName}.yml", true, true);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    var webRoot = webBuilder.GetSetting("WEBROOT");
                    if (!string.IsNullOrEmpty(webRoot))
                    {
                        webBuilder.UseWebRoot(webRoot);
                    }
                    webBuilder.UseStartup<Startup>();
                });

            if (args.Contains("--systemd"))
            {
                config.UseSystemd();
            }

            return config;
        }
    }
}