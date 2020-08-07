using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Internal;
using SS14.Auth.Data;

namespace SS14.Auth.Sessions
{
    public sealed class SessionManager
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly RandomNumberGenerator _cryptoRng;
        private readonly ISystemClock _clock;

        public SessionManager(ApplicationDbContext dbContext, RandomNumberGenerator cryptoRng, ISystemClock clock)
        {
            _dbContext = dbContext;
            _cryptoRng = cryptoRng;
            _clock = clock;
        }

        public async Task<SessionToken> RegisterNewSession(SpaceUser user, TimeSpan expireTime)
        {
            var dat = new byte[SessionToken.TokenLength];
            _cryptoRng.GetBytes(dat);
            var token = new SessionToken(dat);
            user.ActiveSessions.Add(new ActiveSession
            {
                Expires = _clock.UtcNow + expireTime,
                Token = dat
            });

            await _dbContext.SaveChangesAsync();
            return token;
        }

        public async Task<SpaceUser> GetUserForSession(SessionToken token)
        {
            var session = await _dbContext.ActiveSessions
                .Include(p => p.SpaceUser)
                .SingleOrDefaultAsync(p => p.Token == token.Token);

            if (session == null)
            {
                // Session does not exist.
                return null;
            }

            if (session.Expires < _clock.UtcNow)
            {
                // Token expired.
                return null;
            }

            return session.SpaceUser;
        }

        public async Task InvalidateSessions(SpaceUser user)
        {
            _dbContext.ActiveSessions.RemoveRange(_dbContext.ActiveSessions.Where(p => p.SpaceUser == user));

            await _dbContext.SaveChangesAsync();
        }
    }
}