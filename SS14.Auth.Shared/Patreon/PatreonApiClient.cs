#nullable enable
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SS14.Auth.Shared.Config;

namespace SS14.Auth.Shared.Patreon;

public sealed record PatreonTokens(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);

public sealed class PatreonApiClient
{
    private const int MaxAttempts = 5;
    private const int PageSize = 500;

    private static readonly string[] WebhookTriggers =
    [
        "members:create",
        "members:update",
        "members:delete",
        "members:pledge:create",
        "members:pledge:update",
        "members:pledge:delete"
    ];

    private readonly HttpClient _http;
    private readonly IOptions<PatreonConfiguration> _cfg;

    public PatreonApiClient(HttpClient http, IOptions<PatreonConfiguration> cfg)
    {
        _http = http;
        _cfg = cfg;
    }

    public async Task<PatreonMemberPage> GetMembersPageAsync(
        string accessToken,
        string campaignId,
        string? cursor,
        CancellationToken cancel = default)
    {
        var url = $"api/oauth2/v2/campaigns/{Uri.EscapeDataString(campaignId)}/members"
                  + "?include=user,currently_entitled_tiers"
                  + "&fields%5Bmember%5D=patron_status"
                  + $"&page%5Bsize%5D={PageSize}";

        if (cursor != null)
            url += $"&page%5Bcursor%5D={Uri.EscapeDataString(cursor)}";

        using var response = await SendAsync(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new("Bearer", accessToken);
            return request;
        }, cancel);

        await using var stream = await response.Content.ReadAsStreamAsync(cancel);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancel);
        return PatreonMemberParser.ParsePage(doc.RootElement);
    }

    public async Task<List<PatreonCampaignInfo>> GetCampaignsAsync(string accessToken, CancellationToken cancel = default)
    {
        using var response = await SendAsync(() =>
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/oauth2/v2/campaigns?include=tiers,creator"
                + "&fields%5Bcampaign%5D=vanity&fields%5Btier%5D=title&fields%5Buser%5D=full_name");
            request.Headers.Authorization = new("Bearer", accessToken);
            return request;
        }, cancel);

        await using var stream = await response.Content.ReadAsStreamAsync(cancel);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancel);
        return PatreonCampaignParser.ParseCampaigns(doc.RootElement);
    }

    public async Task<string> CreateWebhookAsync(
        string accessToken,
        string campaignId,
        string uri,
        CancellationToken cancel = default)
    {
        var body = JsonSerializer.Serialize(new
        {
            data = new
            {
                type = "webhook",
                attributes = new
                {
                    triggers = WebhookTriggers,
                    uri
                },
                relationships = new
                {
                    campaign = new { data = new { type = "campaign", id = campaignId } }
                }
            }
        });

        using var response = await SendAsync(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/oauth2/v2/webhooks")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new("Bearer", accessToken);
            return request;
        }, cancel);

        await using var stream = await response.Content.ReadAsStreamAsync(cancel);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancel);
        return doc.RootElement.GetProperty("data").GetProperty("attributes").GetProperty("secret").GetString()!;
    }

    public async Task<PatreonTokens> RefreshTokenAsync(string refreshToken, CancellationToken cancel = default)
    {
        using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Post, "api/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = _cfg.Value.ClientId,
                ["client_secret"] = _cfg.Value.ClientSecret
            })
        }, cancel);

        await using var stream = await response.Content.ReadAsStreamAsync(cancel);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancel);
        var root = doc.RootElement;

        return new PatreonTokens(
            root.GetProperty("access_token").GetString()!,
            root.GetProperty("refresh_token").GetString()!,
            DateTimeOffset.UtcNow.AddSeconds(root.GetProperty("expires_in").GetInt32()));
    }

    private async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> makeRequest, CancellationToken cancel)
    {
        for (var attempt = 1;; attempt++)
        {
            var response = await _http.SendAsync(makeRequest(), cancel);
            if (response.StatusCode != HttpStatusCode.TooManyRequests || attempt == MaxAttempts)
            {
                response.EnsureSuccessStatusCode();
                return response;
            }

            var delay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(1 << attempt);
            response.Dispose();
            await Task.Delay(delay, cancel);
        }
    }
}
