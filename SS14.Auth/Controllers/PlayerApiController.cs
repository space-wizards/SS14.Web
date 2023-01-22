using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS14.Auth.Shared.Data;

namespace SS14.Auth.Controllers;

[ApiController]
[Route("/api/player/{userId:guid}")]
public sealed class PlayerApiController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    
    public PlayerApiController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("pastNames")]
    public async Task<IActionResult> GetPastNames(Guid userId)
    {
        var player = await _dbContext.Users.Include(u => u.PastAccountNames).SingleOrDefaultAsync(x => x.Id == userId);
        if (player == null)
            return NotFound();

        var responseData = player.PastAccountNames.Select(n => new PastNameResponse(n.PastName, n.ChangeTime));
        return Ok(responseData);
    }

    private sealed record PastNameResponse(string PastName, DateTime ChangeTime);
}