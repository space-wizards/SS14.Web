using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Internal;
using Microsoft.IdentityModel.Tokens;
using SS14.Auth.Responses;
using SS14.Auth.Shared.Data;

namespace SS14.Auth.Controllers
{
    [ApiController]
    [Route("/api/session")]
    public class SessionApiController : ControllerBase
    {
        private static readonly TimeSpan JoinTimeout = TimeSpan.FromSeconds(20);
        private const int HashSize = 32; // SHA-256

        private readonly IConfiguration _configuration;
        private readonly SpaceUserManager _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly PatreonDataManager _patreonDataManager;
        private readonly ISystemClock _clock;

        public SessionApiController(
            IConfiguration configuration,
            SpaceUserManager userManager,
            ApplicationDbContext dbContext,
            ISystemClock clock,
            PatreonDataManager patreonDataManager)
        {
            _configuration = configuration;
            _userManager = userManager;
            _dbContext = dbContext;
            _clock = clock;
            _patreonDataManager = patreonDataManager;
        }

        [Authorize(AuthenticationSchemes = "SS14Auth")]
        [HttpPost("join")]
        public async Task<IActionResult> Join(JoinRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            var hash = Convert.FromBase64String(request.Hash);
            if (hash.Length != HashSize)
            {
                return BadRequest();
            }

            if (await _dbContext.AuthHashes.AnyAsync(p => p.Hash == hash && p.SpaceUserId == user.Id))
            {
                return BadRequest();
            }

            user.AuthHashes.Add(new AuthHash
            {
                Expires = _clock.UtcNow + JoinTimeout,
                Hash = hash
            });

            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("hasJoined")]
        public async Task<IActionResult> HasJoined(Guid userId, string hash)
        {
            var hashBytes = Base64UrlEncoder.DecodeBytes(hash);
            if (hashBytes.Length != HashSize)
            {
                return BadRequest();
            }

            var authHash = await _dbContext.AuthHashes
                .Include(p => p.SpaceUser)
                .SingleOrDefaultAsync(p => p.Hash == hashBytes && p.SpaceUserId == userId);

            if (authHash == null || authHash.Expires < _clock.UtcNow)
            {
                return Ok(new HasJoinedResponse(false, null));
            }

            var userResponse = await QueryApiController.BuildUserResponse(
                _patreonDataManager, authHash.SpaceUser);
            
            var resp = new HasJoinedResponse(true, userResponse);

            _dbContext.AuthHashes.Remove(authHash);

            await _dbContext.SaveChangesAsync();

            return Ok(resp);
        }

        public sealed record JoinRequest(string Hash)
        {
        }

        public sealed record HasJoinedResponse(bool IsValid, QueryUserResponse? UserData)
        {
        }
    }
}