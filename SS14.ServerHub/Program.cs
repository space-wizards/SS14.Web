using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace SS14.ServerHub;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, builder) =>
            {
                var env = context.HostingEnvironment;
                builder.AddYamlFile("appsettings.yml", false, true);
                builder.AddYamlFile($"appsettings.{env.EnvironmentName}.yml", true, true);
                builder.AddYamlFile("appsettings.Secret.yml", true, true);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseUrls("http://localhost:21953");
                webBuilder.UseStartup<Startup>();
            })
            .UseSystemd();
}