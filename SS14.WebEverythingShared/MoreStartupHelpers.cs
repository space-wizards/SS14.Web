using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace SS14.WebEverythingShared;

// Help I already have this type somewhere.
public class MoreStartupHelpers
{
    public static void AddForwardedSupport(IApplicationBuilder app, IConfiguration config)
    {
        var forwardedHeadersOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        };

        foreach (var ip in config.GetSection("ForwardProxies").Get<string[]>() ?? [])
        {
            forwardedHeadersOptions.KnownProxies.Add(IPAddress.Parse(ip));
        }

        app.UseForwardedHeaders(forwardedHeadersOptions);
    }
}