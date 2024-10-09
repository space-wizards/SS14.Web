using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS14.Auth.Responses;
using SS14.Auth.Shared.Data;

namespace SS14.Auth.Controllers;

[ApiController]
[Route("/api/search")]
public class SearchApiController : ControllerBase
{
    private const int MaxLimit = 100;

    private readonly ApplicationDbContext _db;

    public SearchApiController(ApplicationDbContext db)
    {
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
}
