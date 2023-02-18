using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SS14.Auth.Shared.Data;

namespace SS14.Auth.Controllers;

[ApiController]
[Route("/api/discord")]
public class DiscordLinkController : ControllerBase
{
    private readonly SpaceUserManager _userManager;
    private readonly DiscordLoginSessionManager _loginSessionManager;

    public DiscordLinkController(
        SpaceUserManager userManager,
        DiscordLoginSessionManager loginSessionManager)
    {
        _userManager = userManager;
        _loginSessionManager = loginSessionManager;
    }
    
    [Authorize(AuthenticationSchemes = "SS14Auth")]
    [HttpPost("session")]
    public async Task<IActionResult> Generate()
    {
        var user = await _userManager.GetUserAsync(User);
        var session = await _loginSessionManager.RegisterNewSession(user, DiscordLoginSessionManager.DefaultExpireTime);
        return Ok(new DiscordSessionResponse(session.Id, session.Expires));
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(Guid state, string code)
    {
        var user = await _loginSessionManager.GetSessionById(state);

        if (user == null)
            return NotFound("Session not exist or expired");

        if (user.DiscordId != null)
            return BadRequest("⚠️ You already linked Discord with you account.\nAccount can be unlinked in account settings.");

        await _loginSessionManager.LinkDiscord(user, code);

        return Ok("✅ Discord successfully linked to your account!\nYou can now close this page and return to launcher.");
    }
}

public sealed record DiscordSessionResponse(Guid SessionId, DateTimeOffset ExpireTime)
{
}
