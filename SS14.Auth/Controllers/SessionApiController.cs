using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Internal;
using Microsoft.IdentityModel.Tokens;
using SS14.Auth.Data;

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
        private readonly ISystemClock _clock;
        private static readonly TimeSpan SessionLength = TimeSpan.FromHours(1);

        public SessionApiController(
            IConfiguration configuration,
            SpaceUserManager userManager,
            ApplicationDbContext dbContext,
            ISystemClock clock)
        {
            _configuration = configuration;
            _userManager = userManager;
            _dbContext = dbContext;
            _clock = clock;
        }

        [Authorize(AuthenticationSchemes = "SS14Auth")]
        [HttpPost("join")]
        public async Task<IActionResult> Join(JoinRequest request)
        {
            if (request.Hash == null)
            {
                return BadRequest();
            }

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
                return Ok(new HasJoinedResponse {IsValid = false});
            }

            var resp = new HasJoinedResponse
            {
                IsValid = true,
                UserData = new HasJoinedUserData
                {
                    UserName = authHash.SpaceUser.UserName,
                    UserId = userId
                }
            };

            _dbContext.AuthHashes.Remove(authHash);

            await _dbContext.SaveChangesAsync();

            return Ok(resp);
        }

        public sealed class JoinRequest
        {
            public string Hash { get; set; }
        }

        public sealed class HasJoinedResponse
        {
            public bool IsValid { get; set; }
            public HasJoinedUserData UserData { get; set; }
        }

        public sealed class HasJoinedUserData
        {
            public string UserName { get; set; }
            public Guid UserId { get; set; }
        }
    }
}