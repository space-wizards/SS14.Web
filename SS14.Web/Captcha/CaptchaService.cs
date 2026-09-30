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

namespace SS14.Web.Captcha;

public sealed class CaptchaService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<CaptchaOptions> _options;
    private readonly ILogger<CaptchaService> _logger;

    public CaptchaService(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IOptions<CaptchaOptions> options,
        ILogger<CaptchaService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _options = options;
        _logger = logger;
    }

    public async Task<bool> ValidateCaptcha(string response, ModelStateDictionary modelState)
    {
        if (_options.Value.SiteKey == "")
        {
            // Captcha disabled.
            return true;
        }

        if (string.IsNullOrEmpty(response))
        {
            modelState.AddModelError("", "Please confirm the captcha");
            return false;
        }

        var verifyResponse = await VerifyCaptcha(response);
        if (verifyResponse.Success)
            return true;

        // https://developers.cloudflare.com/turnstile/get-started/server-side-validation/#api-response-format
        // Both are user errors about the Captcha having expired.
        if (!verifyResponse.Success && verifyResponse.ErrorCodes.Contains("invalid-input-response") || verifyResponse.ErrorCodes.Contains("timeout-or-duplicate"))
        {
            modelState.AddModelError("", "The Captcha has expired. Please try again");
        }
        else
        {
            modelState.AddModelError("", "Something went wrong with the Captcha! Please try again otherwise contact support.");
        }

        _logger.LogError(
            "Captcha failed to get validated: Success: {Success}, Error Codes: {ErrorCodes}",
            verifyResponse.Success,
            string.Join(", ", verifyResponse.ErrorCodes ?? Array.Empty<string>())
        );

        return false;
    }

    private async Task<CaptchaVerifyResponse> VerifyCaptcha(string response)
    {
        var client = _httpClientFactory.CreateClient(nameof(CaptchaService));
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

        return await resp.Content.ReadFromJsonAsync<CaptchaVerifyResponse>();
    }

    public static void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        services.Configure<CaptchaOptions>(config.GetSection("Captcha"));
        services.AddScoped<CaptchaService>();
        services.AddHttpClient(nameof(CaptchaService));
    }

    private sealed record CaptchaVerifyResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("challenge_ts")] DateTimeOffset ChallengeTs,
        [property: JsonPropertyName("hostname")] string Hostname,
        [property: JsonPropertyName("error-codes")] string[] ErrorCodes
    );
}
