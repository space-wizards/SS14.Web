using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SS14.ServerHub.Data;

namespace SS14.ServerHub.Controllers
{
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
            var infos = (IEnumerable<ServerInfo>?)_configuration.GetSection("Servers").Get<List<ServerInfo>?>() ??
                        Array.Empty<ServerInfo>();

            var dbInfos = await _dbContext.AdvertisedServer
                .Where(s => s.Expires > DateTime.UtcNow)
                .Select(s => new ServerInfo("", s.Address))
                .ToArrayAsync();

            return infos.Concat(dbInfos).DistinctBy(s => s.Address);
        }

        [HttpPost("advertise")]
        public async Task<IActionResult> Advertise([FromBody] ServerAdvertise advertise)
        {
            var options = _options.Value;

            // Validate that the address is valid.
            if (!Uri.TryCreate(advertise.Address, UriKind.Absolute, out var parsedAddress) ||
                string.IsNullOrWhiteSpace(parsedAddress.Host) ||
                parsedAddress.Scheme is not (Ss14UriHelper.SchemeSs14 or Ss14UriHelper.SchemeSs14s))
                return BadRequest("Invalid SS14 URI");

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
            var secretHash = HashSecret(advertise.Secret);
            var existingAddress =
                await _dbContext.AdvertisedServer.SingleOrDefaultAsync(a => a.Address == advertise.Address);

            if (existingAddress != null)
            {
                // Already have an existing server by this address.
                if (!existingAddress.Secret.AsSpan().SequenceEqual(secretHash))
                {
                    // Not the correct secret, you don't own this server, outta here.
                    return Forbid();
                }

                // Update expiry time, do nothing else.
                existingAddress.Expires = DateTime.UtcNow + TimeSpan.FromMinutes(options.AdvertisementExpireMinutes);
            }
            else
            {
                // Remove existing secret if it exists.
                var existingSecret =
                    await _dbContext.AdvertisedServer.SingleOrDefaultAsync(a => a.Secret == secretHash);
                if (existingSecret != null)
                    _dbContext.AdvertisedServer.Remove(existingSecret);

                _dbContext.AdvertisedServer.Add(new AdvertisedServer
                {
                    Address = advertise.Address,
                    Expires = DateTime.UtcNow + TimeSpan.FromMinutes(options.AdvertisementExpireMinutes),
                    Secret = secretHash
                });
            }

            await _dbContext.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("deadvertise")]
        public async Task<IActionResult> DeAdvertise([FromBody] ServerDeAdvertise deAdvertise)
        {
            var secretHash = HashSecret(deAdvertise.Secret);

            var existing = await _dbContext.AdvertisedServer.SingleOrDefaultAsync(a => a.Secret == secretHash);
            if (existing == null)
                return NotFound();

            _dbContext.AdvertisedServer.Remove(existing);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

        private static byte[] HashSecret(string secret)
        {
            return SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        }

        public sealed record ServerInfo(string Name, string Address)
        {
            public ServerInfo() : this(default!, default!)
            {
            }
        }

        public sealed record ServerAdvertise(string Address, string Secret);

        public sealed record ServerDeAdvertise(string Secret);

        // ReSharper disable once ClassNeverInstantiated.Local
        private sealed record ServerStatus(
            [property: JsonPropertyName("name")] string? Name,
            [property: JsonPropertyName("players")]
            int PlayerCount);
    }
}