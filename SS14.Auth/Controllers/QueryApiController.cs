using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SS14.Auth.Responses;
using SS14.Auth.Shared.Data;

namespace SS14.Auth.Controllers;

[ApiController]
[Route("/api/query")]
public class QueryApiController : ControllerBase
{
    private readonly UserManager<SpaceUser> _userManager;
    private readonly PatreonDataManager _patreonDataManager;
    private readonly DiscordDataManager _discordDataManager;

    public QueryApiController(UserManager<SpaceUser> userManager, PatreonDataManager patreonDataManager, DiscordDataManager discordDataManager)
    {
        _userManager = userManager;
        _patreonDataManager = patreonDataManager;
        _discordDataManager = discordDataManager;
    }

    [HttpGet("name")]
    [HttpHead("name")]
    public async Task<IActionResult> QueryByName(string name)
    {
        var user = await _userManager.FindByNameAsync(name);
        return await DoResponse(user);
    }
        
    [HttpGet("userid")]
    [HttpHead("userid")]
    public async Task<IActionResult> QueryByUserId(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return await DoResponse(user);
    }
    
    [HttpGet("discord")]
    [HttpHead("discord")]
    public async Task<IActionResult> QueryByDiscordId(string discordId)
    {
        var user = await _discordDataManager.GetUserByDiscordId(discordId);
        return await DoResponse(user);
    }
    
    internal static async Task<QueryUserResponse> BuildUserResponse(
        PatreonDataManager patreonDataManager,
        DiscordDataManager discordDataManager,
        SpaceUser user)
    {
        var patronTier = await patreonDataManager.GetPatreonTierAsync(user);
        
        return new QueryUserResponse(user.UserName!, user.Id, patronTier, user.DiscordId, user.CreatedTime);
    }

    private async Task<IActionResult> DoResponse(SpaceUser? user)
    {
        if (user == null)
            return NotFound();
            
        return Ok(await BuildUserResponse(_patreonDataManager, _discordDataManager, user));
    }
}
