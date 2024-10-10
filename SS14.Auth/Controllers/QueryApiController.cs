using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public async Task<IActionResult> SearchByPastName(
        [FromQuery] string name,
        [FromQuery] int limit = 10
    )
    {
        if (limit < 1 || limit > MaxLimit)
        {
            return BadRequest($"Limit out of range. Must be between 1 and {MaxLimit}.");
        }

        var users = await _db.PastAccountNames
            .Where(p => p.PastName.Equals(name))
            .Select(p => new PastUsernameSearchResponse(p.SpaceUser.UserName, p.SpaceUser.Id))
            .ToListAsync();

        /*
         The reason why this is not done in the DB query is because the distinct by user ID is not supported by EF Core.
         This may cause performance issues when a past name was used by many users. But I believe this is a rare enough case.
         This also applies for the .Take because we need to distinct first, otherwise a rare edge case could apply where:
         A user has had the same name multiple times, but we only want to show them once. Let's also say a different user had the same name once.
         We enter the edge case where we take only the first user 10 times, and then the second user is never shown.
         The end result would still only be one past name, even though two users had it.

         This is not wanted behavior, so we need to distinct first, then take.
        */

        users = users
            .DistinctBy(u => u.UserId)
            .Take(limit)
            .ToList();

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
