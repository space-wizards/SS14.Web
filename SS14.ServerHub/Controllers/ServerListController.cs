using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SS14.ServerHub.ServerData;
using SS14.ServerHub.Shared;
using SS14.ServerHub.Shared.Data;
using SS14.ServerHub.Utility;

namespace SS14.ServerHub.Controllers;

[ApiController]
[Route("/api/servers")]
public class ServerListController : ControllerBase
{
    private readonly ILogger<ServerListController> _logger;
    private readonly HubDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly IOptions<HubOptions> _options;

    public ServerListController(
        ILogger<ServerListController> logger,
        HubDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        IOptions<HubOptions> options)
    {
        _logger = logger;
        _dbContext = dbContext;
        _options = options;
        _httpClient = httpClientFactory.CreateClient("ServerStatusCheck");
    }

    [EnableCors(CorsPolicies.PolicyHubPublic)]
    [HttpGet]
    public async Task<IEnumerable<ServerInfo>> Get()
    {
        var dbInfos = await _dbContext.AdvertisedServer
            .Where(s => s.Expires > DateTime.UtcNow)
            .Select(s => new ServerInfo(s.Address, s.StatusData == null ? null : new RawJson(s.StatusData), s.InferredTags))
            .ToArrayAsync();

        return dbInfos;
    }
    
    [EnableCors(CorsPolicies.PolicyHubPublic)]
    [HttpGet("info")]
    public async Task<IActionResult> GetServerInfo(string url)
    {
        var dbInfo = await _dbContext.AdvertisedServer
            .Where(s => s.Expires > DateTime.UtcNow)
            .Where(s => s.Address == url)
            .SingleOrDefaultAsync();

        if (dbInfo == null)
            return NotFound();
        
        return Ok((RawJson?) dbInfo.InfoData);
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
                    "Advertise request sender {Address} is banned (community: {CommunityName})",
                    senderIp,
                    ban.TrackedCommunity.Name);

                return Unauthorized();
            }
        }

        // Validate that the address is valid.
        if (!Uri.TryCreate(advertise.Address, UriKind.Absolute, out var parsedAddress) ||
            string.IsNullOrWhiteSpace(parsedAddress.Host) ||
            parsedAddress.Scheme is not (Ss14UriHelper.SchemeSs14 or Ss14UriHelper.SchemeSs14s))
            return BadRequest("Invalid SS14 URI. Ensure your hub.server_url starts with ss14:// or ss14s:// and that the address is valid.");

        // Ban check.
        switch (await CheckAddressBannedAsync(parsedAddress))
        {
            case BanCheckResult.Banned:
                return Unauthorized("Your server has been blocked from advertising on the hub. If you believe this to be in error, please contact us.");
            case BanCheckResult.FailedResolve:
                return UnprocessableEntity("Server host name failed to resolve");
        }

        var (result, statusJson, infoJson) = await QueryServerStatus(parsedAddress);
        if (result != null)
            return result;

        Debug.Assert(statusJson != null);
        Debug.Assert(infoJson != null);

        switch (await CheckInfoBannedAsync(parsedAddress, statusJson, infoJson))
        {
            case BanCheckResult.Banned:
                return Unauthorized("Your server has been blocked from advertising on the hub. If you believe this to be in error, please contact us.");
        }

        var inferredTags = InferTags(statusJson);

        // Check if a server with this address already exists.
        var addressEntity =
            await _dbContext.AdvertisedServer.SingleOrDefaultAsync(a => a.Address == advertise.Address);

        var timeNow = DateTime.UtcNow;
        var newExpireTime = timeNow + TimeSpan.FromMinutes(options.AdvertisementExpireMinutes);
        if (addressEntity == null)
        {
            addressEntity = new AdvertisedServer
            {
                Address = advertise.Address,
            };
            _dbContext.AdvertisedServer.Add(addressEntity);
        }

        addressEntity.Expires = newExpireTime;
        addressEntity.StatusData = statusJson;
        addressEntity.InfoData = infoJson;
        addressEntity.AdvertiserAddress = senderIp;
        addressEntity.InferredTags = inferredTags;

        _dbContext.ServerStatusArchive.Add(new ServerStatusArchive
        {
            Time = timeNow,
            AdvertisedServer = addressEntity,
            AdvertiserAddress = senderIp,
            StatusData = statusJson,
            InferredTags = inferredTags
        });
        
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    private async Task<(IActionResult? result, byte[]? statusJson, byte[]? infoJson)> QueryServerStatus(Uri uri)
    {
        try
        {
            var options = _options.Value;
            var timeout = TimeSpan.FromSeconds(options.AdvertisementStatusTestTimeoutSeconds);
            var cts = new CancellationTokenSource(timeout);

            // Fetch /status and ensure it's valid (at least a name).
            var maxStatusSize = _options.Value.MaxStatusResponseSize;
            byte[] statusResponse;
            try
            {
                statusResponse = await _httpClient.GetLimitedJsonResponseBody(
                    Ss14UriHelper.GetServerStatusAddress(uri),
                    maxStatusSize * 1024,
                    cts.Token);
            }
            catch (HttpClientHelper.ResponseTooLargeException)
            {
                return (UnprocessableEntity($"/status response data was too large (max: {maxStatusSize} KiB)"), null, null);
            }
            
            var statusData = JsonSerializer.Deserialize<ServerStatus>(statusResponse);
            if (statusData == null)
                throw new InvalidDataException("Status cannot be null");
            
            if (string.IsNullOrWhiteSpace(statusData.Name))
                return (UnprocessableEntity("Server name cannot be empty"), null, null);

            // Fetch /info and just pass it through (no validation except making sure the JSON is well-formed).
            var maxInfoSize = _options.Value.MaxInfoResponseSize;
            byte[] infoResponse;
            try
            {
                var uriBuilder = new UriBuilder(Ss14UriHelper.GetServerInfoAddress(uri));
                uriBuilder.Query = "?can_skip_build=1";
                infoResponse = await _httpClient.GetLimitedJsonResponseBody(
                    new Uri(uriBuilder.ToString()),
                    maxInfoSize * 1024,
                    cts.Token);
            }
            catch (HttpClientHelper.ResponseTooLargeException)
            {
                return (UnprocessableEntity($"/info response data was too large (max: {maxInfoSize} KiB)"), null, null);
            }

            try
            {
                JsonHelper.CheckJsonValid(infoResponse);
            }
            catch (JsonException)
            {
                return (UnprocessableEntity("/info response data was not valid JSON!"), null, null);
            }

            return (null, statusResponse, infoResponse);
        }
        catch (Exception e)
        {
            _logger.LogInformation(e, $"Failed to connect to advertising server: {uri}");
            return (UnprocessableEntity("Unable to contact status address, ensure your firewall/port forwarding configuration allows traffic from the internet and double check your config."), null, null);
        }
    }

    private async Task<BanCheckResult> CheckAddressBannedAsync(Uri uri)
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(_options.Value.AdvertisementBanCheckTimeout));
        var matched = new List<TrackedCommunity>();
        try
        {
            await CommunityMatcher.MatchCommunities(_dbContext, uri, matched, cts.Token);
        }
        catch (CommunityMatcher.FailedResolveException e)
        {
            _logger.LogInformation(e, "{Host} failed to resolve", uri.Host);
            return BanCheckResult.FailedResolve;
        }
        catch (OperationCanceledException e)
        {
            _logger.LogInformation(e, "Checking banned for {Host} timed out", uri.Host);
            return BanCheckResult.Timeout;
        }

        return CheckMatchedCommunitiesForBan(uri, matched);
    }

    private async Task<BanCheckResult> CheckInfoBannedAsync(Uri address, byte[] statusJson, byte[] infoJson)
    {
        var matched = new List<TrackedCommunity>();

        await CommunityMatcher.MatchCommunitiesInfo(_dbContext, statusJson, infoJson, matched);

        return CheckMatchedCommunitiesForBan(address, matched);
    }

    private BanCheckResult CheckMatchedCommunitiesForBan(Uri address, List<TrackedCommunity> matched)
    {
        var banned = matched.FirstOrDefault(x => x.IsBanned);
        if (banned != null)
        {
            _logger.LogInformation(
                "{Host} is banned (community: {CommunityName})", address, banned.Name);
            return BanCheckResult.Banned;
        }

        return BanCheckResult.NotBanned;
    }

    private async Task<TrackedCommunityAddress?> CheckIpBannedAsync(IPAddress address)
    {
        return await CommunityMatcher.CheckIP(_dbContext, address)
            .SingleOrDefaultAsync(b => b.TrackedCommunity.IsBanned);
    }

    private static string[] InferTags(byte[] statusDataJson)
    {
        var statusData = JsonSerializer.Deserialize<ServerStatus>(statusDataJson)!;

        return ServerTagInfer.InferTags(statusData.Name!, statusData.Tags ?? Array.Empty<string>());
    }

    private enum BanCheckResult
    {
        Banned,
        NotBanned,
        FailedResolve,
        Timeout
    }

    public sealed record ServerInfo(string Address, RawJson? StatusData, string[] InferredTags);
    public sealed record ServerAdvertise(string Address);

    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed record ServerStatus(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("players")]
        int PlayerCount,
        [property: JsonPropertyName("tags")]
        string[]? Tags);

    [JsonConverter(typeof(RawJsonConverter))]
    public sealed record RawJson(byte[] Json)
    {
        public static implicit operator RawJson?(byte[]? a) => a == null ? null : new RawJson(a);
    }

    public sealed class RawJsonConverter : JsonConverter<RawJson>
    {
        public override RawJson Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }

        public override void Write(Utf8JsonWriter writer, RawJson value, JsonSerializerOptions options)
        {
            writer.WriteRawValue(value.Json, skipInputValidation: true);
        }
    }
}
