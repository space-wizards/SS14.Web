using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SS14.Auth.Shared.Config;
using SS14.Auth.Shared.Data;
using SS14.Auth.Shared.Patreon;

namespace SS14.Web;

public sealed class PatreonCampaignConnectHandler
{
    public const string ResultConnected = "connected";
    public const string ResultNoCampaign = "no-campaign";
    public const string ResultTaken = "taken";
    public const string ResultLimit = "limit";
    public const string ResultApiError = "api-error";
    public const string ResultImportFailed = "import-failed";
    public const string ResultNotConfigured = "not-configured";

    private readonly SpaceUserManager _userManager;
    private readonly ApplicationDbContext _db;
    private readonly PatreonApiClient _api;
    private readonly PatreonImporter _importer;
    private readonly PatreonSecretProtector _secretProtector;
    private readonly IOptions<PatreonConfiguration> _patreonCfg;
    private readonly ILogger<PatreonCampaignConnectHandler> _logger;

    public PatreonCampaignConnectHandler(
        SpaceUserManager userManager,
        ApplicationDbContext db,
        PatreonApiClient api,
        PatreonImporter importer,
        PatreonSecretProtector secretProtector,
        IOptions<PatreonConfiguration> patreonCfg,
        ILogger<PatreonCampaignConnectHandler> logger)
    {
        _userManager = userManager;
        _db = db;
        _api = api;
        _importer = importer;
        _secretProtector = secretProtector;
        _patreonCfg = patreonCfg;
        _logger = logger;
    }

    public async Task HookReceivedTicket(TicketReceivedContext context)
    {
        var guid = context.Properties.Items["SS14UserId"]!;
        var user = await _userManager.FindByIdAsync(guid);

        if (user == null)
        {
            throw new InvalidOperationException("Unable to find user??");
        }

        var result = await ConnectAsync(context.Properties, user);

        if (context.ReturnUri != null)
        {
            context.HttpContext.Response.Redirect(QueryHelpers.AddQueryString(context.ReturnUri, "result", result));
        }

        context.HandleResponse();
    }

    private async Task<string> ConnectAsync(AuthenticationProperties properties, SpaceUser user)
    {
        var webhookBaseUrl = _patreonCfg.Value.WebhookBaseUrl;
        var accessToken = properties.GetTokenValue("access_token");
        var refreshToken = properties.GetTokenValue("refresh_token");
        if (string.IsNullOrEmpty(webhookBaseUrl) || accessToken == null || refreshToken == null)
            return ResultNotConfigured;

        PatreonCampaignInfo info;
        try
        {
            var campaigns = await _api.GetCampaignsAsync(accessToken);
            info = campaigns.FirstOrDefault();
        }
        catch (HttpRequestException e)
        {
            _logger.LogWarning(e, "Listing campaigns of connecting creator failed");
            return ResultApiError;
        }

        if (info == null)
            return ResultNoCampaign;

        var campaign = await _db.PatreonCampaigns.SingleOrDefaultAsync(c => c.PatreonCampaignId == info.Id);
        if (campaign != null && campaign.OwnerUserId != user.Id)
            return ResultTaken;

        var isNew = campaign == null;
        if (isNew && await _db.PatreonCampaigns.AnyAsync(c => c.OwnerUserId == user.Id))
            return ResultLimit;

        campaign ??= new PatreonCampaign
        {
            Key = await MakeUniqueKeyAsync(info.Name),
            PatreonCampaignId = info.Id,
            Name = info.Name,
            TierMap = info.Tiers.ToDictionary(t => t.Key, t => t.Value),
            TierNames = info.Tiers.Values.Distinct().ToDictionary(t => t, t => t),
            OwnerUserId = user.Id,
            Enabled = false
        };

        if (campaign.WebhookSecret == null)
        {
            try
            {
                var uri = $"{webhookBaseUrl.TrimEnd('/')}/hook/patreon/{campaign.Key}";
                var secret = await _api.CreateWebhookAsync(accessToken, info.Id, uri);
                campaign.WebhookSecret = _secretProtector.Protect(secret);
            }
            catch (HttpRequestException e)
            {
                _logger.LogWarning(e, "Creating webhook for campaign {Campaign} failed", info.Id);
                return ResultApiError;
            }
        }

        campaign.AccessToken = _secretProtector.Protect(accessToken);
        campaign.RefreshToken = _secretProtector.Protect(refreshToken);
        campaign.TokenExpiresAt = properties.GetTokenValue("expires_at") is { } expires
            ? DateTimeOffset.Parse(expires, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            : null;

        if (isNew)
            _db.PatreonCampaigns.Add(campaign);

        await _db.SaveChangesAsync();

        try
        {
            await _importer.ImportAsync(campaign);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Importing members of campaign {Campaign} failed", campaign.Key);
            return ResultImportFailed;
        }

        campaign.Enabled = true;
        await _db.SaveChangesAsync();

        return ResultConnected;
    }

    private async Task<string> MakeUniqueKeyAsync(string name)
    {
        var baseKey = MakeKey(name);
        var key = baseKey;
        for (var i = 2; await _db.PatreonCampaigns.AnyAsync(c => c.Key == key); i++)
        {
            key = $"{baseKey}-{i}";
        }

        return key;
    }

    internal static string MakeKey(string name)
    {
        var key = Regex.Replace(name.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        if (key.Length > 40)
            key = key[..40].TrimEnd('-');

        return key == "" ? "campaign" : key;
    }
}
