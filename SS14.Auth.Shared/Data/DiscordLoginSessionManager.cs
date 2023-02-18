using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SS14.Auth.Shared.Config;

namespace SS14.Auth.Shared.Data;

public sealed class DiscordLoginSessionManager
{
    public static readonly TimeSpan DefaultExpireTime = TimeSpan.FromMinutes(5);
    
    private readonly ApplicationDbContext _db;
    private readonly ISystemClock _clock;
    private readonly HttpClient _httpClient;
    private readonly IOptions<DiscordConfiguration> _config;
    private readonly ILogger<DiscordLoginSessionManager> _logger;

    public DiscordLoginSessionManager(
        ApplicationDbContext db,
        ISystemClock clock,
        IHttpClientFactory httpClientFactory,
        IOptions<DiscordConfiguration> config,
        ILogger<DiscordLoginSessionManager> logger)
    {
        _db = db;
        _clock = clock;
        _httpClient = httpClientFactory.CreateClient(nameof(DiscordLoginSessionManager));
        _config = config;
        _logger = logger;
    }

    public async Task<DiscordLoginSession> RegisterNewSession(SpaceUser user, TimeSpan expireTime)
    {
        var expiresAt = _clock.UtcNow + expireTime;
        var session = new DiscordLoginSession
        {
            SpaceUserId = user.Id,
            Expires = expiresAt,
        };
        _db.DiscordLoginSessions.Add(session);
        await _db.SaveChangesAsync();
        return session;
    }
    
    public async Task<SpaceUser> GetSessionById(Guid sessionId)
    {
        var session = await _db.DiscordLoginSessions
            .Include(p => p.SpaceUser)
            .Include(p => p.SpaceUser.Discord)
            .SingleOrDefaultAsync(s => s.Id == sessionId);
        
        if (session == null)
        {
            // Session does not exist.
            return null;
        }

        if (session.Expires < _clock.UtcNow)
        {
            // Token expired.
            return null;
        }

        return session.SpaceUser;
    }

    public async Task LinkDiscord(SpaceUser user, string discordCode)
    {
        var accessToken = await ExchangeDiscordCode(discordCode);
        var discordId = await GetDiscordId(accessToken);
        user.Discord = new Discord
        {
            DiscordId = discordId,
        };
        await _db.SaveChangesAsync();
        _logger.LogInformation("User {UserId} linked to {DiscordId} Discord", user.Id, discordId);
    }

    private async Task<string> ExchangeDiscordCode(string discordCode)
    {
        var config = _config.Value;
        
        var exchangeParams = new List<KeyValuePair<string, string>>(5)
        {
            new("client_id", config.ClientId),
            new("client_secret", config.ClientSecret),
            new("redirect_uri", config.RedirectUri),
            new("grant_type", "authorization_code"),
            new("code", discordCode),
        };
        var form = new FormUrlEncodedContent(exchangeParams);
        var resp = await _httpClient.PostAsync("https://discord.com/api/v10/oauth2/token", form);
        resp.EnsureSuccessStatusCode();

        var data = await resp.Content.ReadFromJsonAsync<DiscordExchangeResponse>();
        if (data == null)
            throw new InvalidDataException("Response data cannot be null");
        
        return data.AccessToken;
    }

    private async Task<string> GetDiscordId(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/v10/users/@me");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        var resp = await _httpClient.SendAsync(request);
        resp.EnsureSuccessStatusCode();

        var data = await resp.Content.ReadFromJsonAsync<DiscordMeResponse>();
        if (data == null)
            throw new InvalidDataException("Response data cannot be null");
        
        return data.Id;
    }
    
    private sealed record DiscordExchangeResponse(
        [property: JsonPropertyName("access_token")] string AccessToken
    );
    
    private sealed record DiscordMeResponse(
        [property: JsonPropertyName("id")] string Id
    );
}

