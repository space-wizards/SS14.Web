using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SS14.Auth.Shared.Data;

namespace SS14.Web
{
    public sealed class DiscordConnectionHandler
    {
        private readonly UserManager<SpaceUser> _userManager;
        private readonly ApplicationDbContext _db;

        public DiscordConnectionHandler(
            UserManager<SpaceUser> userManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }
        public async Task HookReceivedTicket(TicketReceivedContext context)
        {
            var guid = context.Properties.Items["SS14UserId"]!;
            var user = await _userManager.FindByIdAsync(guid);
            if (user == null)
            {
                throw new InvalidOperationException("Unable to find user on hook");
            }
            
            var discordId = context.Principal!.Claims.First(p => p.Type == ClaimTypes.NameIdentifier).Value;
            var existingDiscord = await _db.Discords.FirstOrDefaultAsync(a => a.DiscordId == discordId);
            if (existingDiscord != null)
            {
                // Relinking
                _db.Discords.Remove(existingDiscord);
            }
            
            existingDiscord = await _db.Discords.FirstOrDefaultAsync(a => a.SpaceUserId == user.Id);
            if (existingDiscord != null)
            {
                // Relinking
                _db.Discords.Remove(existingDiscord);
            }

            var discordLink = new Discord
            {
                DiscordId = discordId,
                SpaceUserId = user.Id
            };
            _db.Discords.Add(discordLink);
            await _db.SaveChangesAsync();

            if (context.ReturnUri != null)
            {
                context.HttpContext.Response.Redirect(context.ReturnUri);
            }

            context.HandleResponse();
        }
    }
}