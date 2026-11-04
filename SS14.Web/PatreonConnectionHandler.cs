using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SS14.Auth.Shared.Data;

namespace SS14.Web;

public sealed class PatreonConnectionHandler
{
    private readonly SpaceUserManager _userManager;
    private readonly ApplicationDbContext _db;
    private readonly AccountLogManager _accountLogManager;

    public PatreonConnectionHandler(
        SpaceUserManager userManager,
        ApplicationDbContext db,
        AccountLogManager accountLogManager)
    {
        _userManager = userManager;
        _db = db;
        _accountLogManager = accountLogManager;
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

        var newPatron = new Patron
        {
            PatreonId = patreonId,
            SpaceUserId = user.Id
        };

        // ReSharper disable once MethodHasAsyncOverload
        _db.Patrons.Add(newPatron);

        await _accountLogManager.LogAndSave(
            user,
            new AccountLogPatreonLinked(),
            _accountLogManager.ActorWithIP(user));

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        if (context.ReturnUri != null)
        {
            context.HttpContext.Response.Redirect(context.ReturnUri);
        }

        context.HandleResponse();
    }
}