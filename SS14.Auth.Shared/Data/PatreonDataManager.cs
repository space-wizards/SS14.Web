using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SS14.Auth.Shared.Data;

public sealed class PatreonDataManager
{
    private readonly ApplicationDbContext _db;

    public PatreonDataManager(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<string> GetPatreonTierAsync(SpaceUser user)
    {
        return (await _db.Patrons.SingleOrDefaultAsync(p => p.SpaceUserId == user.Id))?.CurrentTier;
    }
}