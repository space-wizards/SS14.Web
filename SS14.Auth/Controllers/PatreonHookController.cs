using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SS14.Auth.Shared.Config;
using SS14.Auth.Shared.Data;
using SS14.Auth.Shared.Patreon;

namespace SS14.Auth.Controllers;

[ApiController]
[Route("/hook/patreon")]
public class PatreonHookController : ControllerBase
{
    private readonly IOptions<PatreonConfiguration> _patreonCfg;
    private readonly ILogger<PatreonHookController> _logger;
    private readonly ApplicationDbContext _db;
    private readonly PatreonMembershipManager _membershipManager;
    private readonly PatreonSecretProtector _secretProtector;

    public PatreonHookController(
        IOptions<PatreonConfiguration> patreonCfg,
        ILogger<PatreonHookController> logger,
        ApplicationDbContext db,
        PatreonMembershipManager membershipManager,
        PatreonSecretProtector secretProtector)
    {
        _patreonCfg = patreonCfg;
        _logger = logger;
        _db = db;
        _membershipManager = membershipManager;
        _secretProtector = secretProtector;
    }

    [HttpPost("{campaignKey}")]
    public async Task<IActionResult> CampaignHook(
        string campaignKey,
        [FromHeader(Name = "X-Patreon-Signature")] string? signature,
        [FromHeader(Name = "X-Patreon-Event")] string? trigger)
    {
        // Not filtered on Enabled so events aren't lost while the first import is still pending.
        var campaign = await _db.PatreonCampaigns.SingleOrDefaultAsync(c => c.Key == campaignKey);
        if (campaign?.WebhookSecret == null)
            return NotFound();

        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms);
        var body = ms.ToArray();

        var secret = _secretProtector.Unprotect(campaign.WebhookSecret);
        if (!PatreonWebhookSignature.Verify(body, secret, signature))
        {
            _logger.LogDebug("Signature failed to validate for campaign {Campaign}, ignoring", campaign.Key);
            return Unauthorized();
        }

        campaign.LastWebhookAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        if (_patreonCfg.Value.LogWebhooks)
        {
            _db.PatreonWebhookLogs.Add(new PatreonWebhookLog
            {
                Content = Encoding.UTF8.GetString(body),
                Time = DateTimeOffset.UtcNow,
                Trigger = trigger ?? "",
                CampaignId = campaign.Id
            });

            await _db.SaveChangesAsync();
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            return BadRequest();
        }

        using (doc)
        {
            var member = PatreonMemberParser.ParseWebhook(doc.RootElement);
            if (member == null)
                return Ok();

            switch (trigger)
            {
                case "members:create":
                case "members:update":
                case "members:pledge:create":
                case "members:pledge:update":
                case "members:pledge:delete":
                    await _membershipManager.UpsertAsync(campaign, member);
                    break;

                case "members:delete":
                    await _membershipManager.RemoveAsync(campaign, member.UserId);
                    break;
            }
        }

        return Ok();
    }
}