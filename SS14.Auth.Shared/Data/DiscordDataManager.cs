﻿using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SS14.Auth.Shared.Data
{
    public sealed class DiscordDataManager
    {
        private readonly ApplicationDbContext _db;

        public DiscordDataManager(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<string> GetUserDiscordId(SpaceUser user)
        {
            return (await _db.Discords.SingleOrDefaultAsync(p => p.SpaceUserId == user.Id))?.DiscordId;
        }

        public async Task<SpaceUser> GetUserByDiscordId(string discordId)
        {
            return (await _db.Discords.Include(d => d.SpaceUser).SingleOrDefaultAsync(p => p.DiscordId == discordId))?.SpaceUser;
        }
    }
}