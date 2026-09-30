using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SS14.Web.HCaptcha;

public sealed class HCaptchaService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<HCaptchaOptions> _options;
    private readonly ILogger<HCaptchaService> _logger;

    public HCaptchaService(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IOptions<HCaptchaOptions> options,
        ILogger<HCaptchaService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _options = options;
        _logger = logger;
    }

    public async Task<bool> ValidateHCaptcha(string response, ModelStateDictionary modelState)
    {
        if (_options.Value.SiteKey == "")
        {
            // hCaptcha disabled.
            return true;
        }

        if (string.IsNullOrEmpty(response))
        {
            modelState.AddModelError("", "Please confirm the captcha");
            return false;
        }

        var verifyResponse = await VerifyHCaptcha(response);
        if (verifyResponse.Success)
            return true;

        modelState.AddModelError("", "Failed to verify captcha response.");

        _logger.LogError(
            "Captcha failed to get validated server side: Success: {Success}, Challenge Time: {ChallengeTS}, Hostname: {Hostname}, Credit: {Credit}, Error Codes: {ErrorCodes}",
            verifyResponse.Success,
            verifyResponse.ChallengeTS,
            verifyResponse.Hostname,
            verifyResponse.Credit,
            string.Join(", ", verifyResponse.ErrorCodes ?? Array.Empty<string>())
        );

        return false;
    }

    private async Task<HCaptchaVerifyResponse> VerifyHCaptcha(string response)
    {
        var client = _httpClientFactory.CreateClient(nameof(HCaptchaService));
        var options = _options.Value;

        var verifyParams = new List<KeyValuePair<string, string>>(4)
        {
            new("response", response),
            new("secret", options.Secret),
            new("sitekey", options.SiteKey)
        };

        if (_httpContextAccessor.HttpContext?.Connection.RemoteIpAddress is { } ip)
            verifyParams.Add(new ("remoteip", ip.ToString()));

        var content = new FormUrlEncodedContent(verifyParams);

        var resp = await client.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", content);
        resp.EnsureSuccessStatusCode();

        return await resp.Content.ReadFromJsonAsync<HCaptchaVerifyResponse>();
    }

    public static void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        services.Configure<HCaptchaOptions>(config.GetSection("HCaptcha"));
        services.AddScoped<HCaptchaService>();
        services.AddHttpClient(nameof(HCaptchaService));
    }

    private sealed record HCaptchaVerifyResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("challenge_ts")] DateTimeOffset ChallengeTS,
        [property: JsonPropertyName("hostname")] string Hostname,
        [property: JsonPropertyName("credit")] bool Credit,
        [property: JsonPropertyName("error-codes")] string[] ErrorCodes
    );
}
