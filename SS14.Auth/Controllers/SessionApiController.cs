using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Internal;
using Microsoft.IdentityModel.Tokens;
using SS14.Auth.Responses;
using SS14.Auth.Shared.Data;

namespace SS14.Auth.Controllers;

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
        var user = (await _userManager.GetUserAsync(User))!;
        var hash = Convert.FromBase64String(request.Hash);
        if (hash.Length != HashSize)
        {
            return BadRequest();
        }

        if (await _dbContext.AuthHashes.AnyAsync(p => p.Hash == hash && p.SpaceUserId == user.Id))
        {
            return BadRequest();
        }

        // Get the HWID here, then give it to the server later on hasJoined.
        var hwidHit = await GetOrAddHwid(user, request.Hwid);

        user.AuthHashes.Add(new AuthHash
        {
            Expires = _clock.UtcNow + JoinTimeout,
            Hash = hash,
            Hwid = hwidHit,
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
            .Include(p => p.Hwid)
            .SingleOrDefaultAsync(p => p.Hash == hashBytes && p.SpaceUserId == userId);

        if (authHash == null || authHash.Expires < _clock.UtcNow)
        {
            return Ok(new HasJoinedResponse(false, null, null));
        }

        var userResponse = await QueryApiController.BuildUserResponse(
            _patreonDataManager, authHash.SpaceUser);

        var hwidString = authHash.Hwid?.Value is { } hwid ? Convert.ToBase64String(hwid) : null;

        var resp = new HasJoinedResponse(
            true,
            userResponse,
            new HasJoinedConnectionData(hwidString == null ? [] : [hwidString], 0.5f));

        _dbContext.AuthHashes.Remove(authHash);

        await _dbContext.SaveChangesAsync();

        return Ok(resp);
    }

    private async Task<Hwid?> GetOrAddHwid(SpaceUser user, string? hwid)
    {
        if (hwid == null)
            return null;

        // Invalid HWID data -> null.
        if (!TryFromBase64String(hwid, out var hwidData))
            return null;

        var existing = await _dbContext.Hwids.SingleOrDefaultAsync(a => a.ClientData == hwidData);
        if (existing == null)
        {
            existing = new Hwid
            {
                ClientData = hwidData,
                TypeCode = Hwid.Type1,
                Value = GenerateHwid(),
            };

            _dbContext.Hwids.Add(existing);
        }

        var hwidUser = await _dbContext.HwidUsers.SingleOrDefaultAsync(
            u => u.SpaceUser == user && u.HwidId == existing.Id);

        if (hwidUser == null)
        {
            _dbContext.HwidUsers.Add(new HwidUser
            {
                Hwid = existing,
                FirstSeen = DateTime.UtcNow,
                SpaceUser = user,
            });
        }

        return existing;
    }

    private static byte[] GenerateHwid()
    {
        const int hwidLength = 32;

        return RandomNumberGenerator.GetBytes(hwidLength);
    }

    private static bool TryFromBase64String(string base64, [NotNullWhen(true)] out byte[]? data)
    {
        try
        {
            data = Convert.FromBase64String(base64);
            return true;
        }
        catch (FormatException)
        {
            data = null;
            return false;
        }
    }

    public sealed record JoinRequest(string Hash, string? Hwid)
    {
    }

    public sealed record HasJoinedResponse(
        bool IsValid,
        QueryUserResponse? UserData,
        HasJoinedConnectionData? ConnectionData);

    public sealed record HasJoinedConnectionData(string[] Hwids, float Trust);
}
