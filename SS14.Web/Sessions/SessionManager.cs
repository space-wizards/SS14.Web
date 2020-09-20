using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Internal;
using SS14.Web.Data;

namespace SS14.Web.Sessions
{
    public sealed class SessionManager
    {
        public static readonly TimeSpan DefaultExpireTime = TimeSpan.FromDays(30);

        private readonly ApplicationDbContext _dbContext;
        private readonly RandomNumberGenerator _cryptoRng;
        private readonly ISystemClock _clock;

        public SessionManager(ApplicationDbContext dbContext, RandomNumberGenerator cryptoRng, ISystemClock clock)
        {
            _dbContext = dbContext;
            _cryptoRng = cryptoRng;
            _clock = clock;
        }

        public async Task<(SessionToken token, DateTimeOffset expireTime)> RegisterNewSession(SpaceUser user,
            TimeSpan expireTime)
        {
            var dat = new byte[SessionToken.TokenLength];
            _cryptoRng.GetBytes(dat);
            var token = new SessionToken(dat);
            var expires = _clock.UtcNow + expireTime;
            user.LoginSessions.Add(new LoginSession
            {
                Expires = expires,
                Token = dat
            });

            await _dbContext.SaveChangesAsync();
            return (token, expires);
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

        public async Task InvalidateToken(SessionToken token)
        {
            var session = await _dbContext.ActiveSessions.SingleOrDefaultAsync(p => p.Token == token.Token);

            if (session == null)
            {
                return;
            }

            _dbContext.ActiveSessions.Remove(session);
            await _dbContext.SaveChangesAsync();
        }

        /// <returns>Null if the provided token was not valid.</returns>
        public async Task<(SessionToken token, DateTimeOffset expireTime)?> RefreshToken(SessionToken token)
        {
            var session = await _dbContext.ActiveSessions
                .Include(p => p.SpaceUser)
                .SingleOrDefaultAsync(p => p.Token == token.Token);

            if (session == null)
            {
                return null;
            }

            if (session.Expires < _clock.UtcNow)
            {
                // Token expired.
                return null;
            }

            var user = session.SpaceUser;

            // Remove old token.
            _dbContext.ActiveSessions.Remove(session);

            return await RegisterNewSession(user, DefaultExpireTime);
        }
    }
}