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
    private readonly UserManager<SpaceUser> _userManager;
    private readonly ApplicationDbContext _db;

    public SearchApiController(UserManager<SpaceUser> userManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    /// <summary>
    /// Returns 10 accounts that match the search query.
    /// </summary>
    [HttpGet("name")]
    [HttpHead("name")]
    public IActionResult SearchByName(
        string name
    )
    {
        var users = _userManager.Users
            .Where(u => u.UserName != null && u.UserName.Contains(name))
            .Take(10)
            .Select(u => u.UserName);

        return Ok(users);
    }

    /// <summary>
    /// Returns a max of 10 accounts which had the given name in the past.
    /// </summary>
    [HttpGet("pastname")]
    [HttpHead("pastname")]
    public IActionResult SearchByPastName(
        string name
    )
    {
        var users = _db.PastAccountNames
            .Where(p => p.PastName.Equals(name))
            .Select(p => new PastUsernameSearchResponse(p.SpaceUser.UserName, p.SpaceUser.Id))
            .ToList()
            .DistinctBy(p => p.UserId)
            .Take(10);

        return Ok(users);
    }
}
