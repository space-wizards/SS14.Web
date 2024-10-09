using System;
using System.Linq;
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
    private const int MaxLimit = 100;

    private readonly UserManager<SpaceUser> _userManager;
    private readonly PatreonDataManager _patreonDataManager;
    private readonly ApplicationDbContext _db;

    public QueryApiController(UserManager<SpaceUser> userManager, PatreonDataManager patreonDataManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _patreonDataManager = patreonDataManager;
        _db = db;
    }

    /// <summary>
    /// Returns accounts which had the given name in the past with a configurable limit.
    /// </summary>
    [HttpGet("pastname")]
    [HttpHead("pastname")]
    public IActionResult SearchByPastName(
        [FromQuery] string name,
        [FromQuery] int limit = 10
    )
    {
        if (limit < 1 || limit > MaxLimit)
        {
            return BadRequest($"Limit out of range. Must be between 1 and {MaxLimit}.");
        }

        var users = _db.PastAccountNames
            .Where(p => p.PastName.Equals(name))
            .Select(p => new PastUsernameSearchResponse(p.SpaceUser.UserName, p.SpaceUser.Id))
            .ToList()
            .DistinctBy(p => p.UserId)
            .Take(limit);

        return Ok(users);
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

    internal static async Task<QueryUserResponse> BuildUserResponse(
        PatreonDataManager patreonDataManager,
        SpaceUser user)
    {
        var patronTier = await patreonDataManager.GetPatreonTierAsync(user);

        return new QueryUserResponse(user.UserName!, user.Id, patronTier, user.CreatedTime);
    }

    private async Task<IActionResult> DoResponse(SpaceUser? user)
    {
        if (user == null)
            return NotFound();

        return Ok(await BuildUserResponse(_patreonDataManager, user));
    }
}
