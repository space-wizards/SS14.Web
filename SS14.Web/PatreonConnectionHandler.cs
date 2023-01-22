using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SS14.Auth.Shared.Config;
using SS14.Auth.Shared.Data;

namespace SS14.Web;

public sealed class PatreonConnectionHandler
{
    private const string ClaimPatreonReward = "PatreonReward";

    private readonly SpaceUserManager _userManager;
    private readonly ApplicationDbContext _db;
    private readonly IOptions<PatreonConfiguration> _patreonCfg;

    public PatreonConnectionHandler(
        SpaceUserManager userManager,
        ApplicationDbContext db,
        IOptions<PatreonConfiguration> patreonCfg)
    {
        _userManager = userManager;
        _db = db;
        _patreonCfg = patreonCfg;
    }

    public Task HookCreatingTicket(OAuthCreatingTicketContext context)
    {
        // Add current tier to the principal, rest is handle in Received.

        var identity = (ClaimsIdentity) context.Principal!.Identity;

        // AFAICT you shouldn't be able to have multiple tiers with our current patreon setup so...
        var tier = ParseTiers(context.User)
            .Where(t => _patreonCfg.Value.TierMap.ContainsKey(t))
            .Select(t => _patreonCfg.Value.TierMap[t])
            .FirstOrDefault();

        if (tier != null)
        {
            identity!.AddClaim(new Claim(ClaimPatreonReward, tier));
        }

        Console.WriteLine(context.AccessToken);

        return Task.CompletedTask;
    }

    public async Task HookReceivedTicket(TicketReceivedContext context)
    {
        var guid = context.Properties.Items["SS14UserId"]!;
        var user = await _userManager.FindByIdAsync(guid);

        if (user == null)
        {
            throw new InvalidOperationException("Unable to find user??");
        }

        await using var tx = await _db.Database.BeginTransactionAsync();
        
        // Ok we have their patreon account and our user ID, time to add it to the database.
        var patreonId = context.Principal!.Claims.First(p => p.Type == ClaimTypes.NameIdentifier).Value;
        var existingPatron = await _db.Patrons.FirstOrDefaultAsync(a => a.PatreonId == patreonId);
        if (existingPatron != null)
        {
            // Somebody trying to use multiple accounts I guess, dis-associate their previous account.
            _db.Patrons.Remove(existingPatron);
        }
            
        existingPatron = await _db.Patrons.FirstOrDefaultAsync(a => a.SpaceUserId == user.Id);
        if (existingPatron != null)
        {
            // Already has a patreon account linked??
            _db.Patrons.Remove(existingPatron);
        }

        var tier = context.Principal.Claims.FirstOrDefault(p => p.Type == ClaimPatreonReward)?.Value; 
            
        var newPatron = new Patron
        {
            CurrentTier = tier,
            PatreonId = patreonId,
            SpaceUserId = user.Id
        };

        // ReSharper disable once MethodHasAsyncOverload
        _db.Patrons.Add(newPatron);
        
        _userManager.LogPatreonLinked(user, user);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        if (context.ReturnUri != null)
        {
            context.HttpContext.Response.Redirect(context.ReturnUri);
        }

        context.HandleResponse();
    }

    [SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
    internal static IEnumerable<string> ParseTiers(JsonElement elemRoot)
    {
        if (!elemRoot.TryGetProperty("included", out var included))
            return Enumerable.Empty<string>();
            
        var includedMemberships = included
            .EnumerateArray()
            .Where(c => c.GetProperty("type").GetString() == "member");

        return elemRoot
            .GetProperty("data")
            .GetProperty("relationships")
            .GetProperty("memberships")
            .GetProperty("data")
            .EnumerateArray()
            .Join(
                includedMemberships,
                m => m.GetProperty("id").GetString(),
                m => m.GetProperty("id").GetString(),
                (_, member) => member)
            .SelectMany(m =>
                m.GetProperty("relationships")
                    .GetProperty("currently_entitled_tiers")
                    .GetProperty("data")
                    .EnumerateArray())
            .Select(m => m.GetProperty("id").GetString());
    }
}