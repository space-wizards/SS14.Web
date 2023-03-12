using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SS14.ServerHub.Data;

namespace SS14.ServerHub.Controllers;

[ApiController]
[Route("/api/servers")]
public class ServerListController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServerListController> _logger;
    private readonly HubDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly IOptions<HubOptions> _options;

    public ServerListController(
        IConfiguration configuration,
        ILogger<ServerListController> logger,
        HubDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        IOptions<HubOptions> options)
    {
        _configuration = configuration;
        _logger = logger;
        _dbContext = dbContext;
        _options = options;
        _httpClient = httpClientFactory.CreateClient("ServerStatusCheck");
    }

    [HttpGet]
    public async Task<IEnumerable<ServerInfo>> Get()
    {
        var dbInfos = await _dbContext.AdvertisedServer
            .Where(s => s.Expires > DateTime.UtcNow)
            .Select(s => new ServerInfo("", s.Address))
            .ToArrayAsync();

        return dbInfos;
    }

    [HttpPost("advertise")]
    public async Task<IActionResult> Advertise([FromBody] ServerAdvertise advertise)
    {
        var options = _options.Value;

        var senderIp = HttpContext.Connection.RemoteIpAddress;
        if (senderIp != null)
        {
            // Check IP ban for request sender (NOT advertised address yet).
            var ban = await CheckIpBannedAsync(senderIp);
            if (ban != null)
            {
                _logger.LogInformation(
                    "Advertise request sender {Address} is banned: {BanReason}",
                    senderIp,
                    ban.Reason);

                return Unauthorized();
            }
        }

        // Validate that the address is valid.
        if (!Uri.TryCreate(advertise.Address, UriKind.Absolute, out var parsedAddress) ||
            string.IsNullOrWhiteSpace(parsedAddress.Host) ||
            parsedAddress.Scheme is not (Ss14UriHelper.SchemeSs14 or Ss14UriHelper.SchemeSs14s))
            return BadRequest("Invalid SS14 URI");

        // Ban check.
        switch (await CheckAddressBannedAsync(parsedAddress))
        {
            case BanCheckResult.Banned:
                return Unauthorized();
            case BanCheckResult.FailedResolve:
                return UnprocessableEntity("Server host name failed to resolve");
        }

        // Validate that we can reach the server.
        try
        {
            var timeout = TimeSpan.FromSeconds(options.AdvertisementStatusTestTimeoutSeconds);
            var cts = new CancellationTokenSource(timeout);

            var response = await _httpClient.GetAsync(
                Ss14UriHelper.GetServerStatusAddress(parsedAddress),
                cts.Token);

            response.EnsureSuccessStatusCode();
            var status = await response.Content.ReadFromJsonAsync<ServerStatus>(cancellationToken: cts.Token);
            if (status == null)
                throw new InvalidDataException("Status cannot be null");

            if (string.IsNullOrWhiteSpace(status.Name))
                return UnprocessableEntity("Server name cannot be empty");
        }
        catch (Exception e)
        {
            _logger.LogInformation(e, "Failed to connect to advertising server");
            return UnprocessableEntity("Unable to contact status address");
        }

        // Check if a server with this address already exists.
        var existingAddress =
            await _dbContext.AdvertisedServer.SingleOrDefaultAsync(a => a.Address == advertise.Address);

        var newExpireTime = DateTime.UtcNow + TimeSpan.FromMinutes(options.AdvertisementExpireMinutes);
        if (existingAddress != null)
        {
            // Update expiry time, do nothing else.
            existingAddress.Expires = newExpireTime;
        }
        else
        {
            _dbContext.AdvertisedServer.Add(new AdvertisedServer
            {
                Address = advertise.Address,
                Expires = newExpireTime,
            });
        }

        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    private async Task<BanCheckResult> CheckAddressBannedAsync(Uri uri)
    {
        var host = uri.Host;

        if (!IPAddress.TryParse(host, out _))
        {
            // If a domain name, check for domain ban.

            var domainBan = await _dbContext.BannedDomain
                .FirstOrDefaultAsync(b => b.DomainName == host || EF.Functions.Like(host, "%." + b.DomainName));

            if (domainBan != null)
            {
                _logger.LogInformation("{Host} is banned: {BanReason}", host, domainBan.Reason);
                return BanCheckResult.Banned;
            }
        }

        IPAddress[] addresses;
        try
        {
            // If the host is an IP address, GetHostAddressesAsync returns it directly.
            addresses = await Dns.GetHostAddressesAsync(host);
        }
        catch (SocketException e)
        {
            // Failure to resolve or something. Could be a mistake or something, so don't report 401.
            _logger.LogInformation(e, "{Host} is failed to resolve", host);
            return BanCheckResult.FailedResolve;
        }

        // Check EVERY address.
        foreach (var checkAddress in addresses)
        {
            var addressBan = await CheckIpBannedAsync(checkAddress);

            if (addressBan != null)
            {
                _logger.LogInformation("{Host} is banned: {BanReason}", host, addressBan.Reason);
                return BanCheckResult.Banned;
            }
        }

        return BanCheckResult.NotBanned;
    }

    private async Task<BannedAddress?> CheckIpBannedAsync(IPAddress address)
    {
        return await _dbContext.BannedAddress
            .SingleOrDefaultAsync(b => EF.Functions.ContainsOrEqual(b.Address, address));
    }

    private enum BanCheckResult
    {
        Banned,
        NotBanned,
        FailedResolve
    }

    public sealed record ServerInfo(string Name, string Address)
    {
        // Used when loading config.
        // ReSharper disable once UnusedMember.Global
        public ServerInfo() : this(default!, default!)
        {
        }
    }

    public sealed record ServerAdvertise(string Address);

    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed record ServerStatus(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("players")]
        int PlayerCount);
}