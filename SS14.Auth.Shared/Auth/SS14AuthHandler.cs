using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using SS14.Auth.Shared.Data;
using SS14.Auth.Shared.Sessions;

namespace SS14.Auth.Shared.Auth
{
    [UsedImplicitly]
    public sealed class SS14AuthHandler : AuthenticationHandler<SS14AuthOptions>
    {
        private readonly SessionManager _sessionManager;
        private readonly SignInManager<SpaceUser> _signInManager;

        public SS14AuthHandler(IOptionsMonitor<SS14AuthOptions> options, ILoggerFactory logger, UrlEncoder encoder,
            ISystemClock clock, SessionManager sessionManager, SignInManager<SpaceUser> signInManager) : base(options,
            logger, encoder, clock)
        {
            _sessionManager = sessionManager;
            _signInManager = signInManager;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string authorization = Request.Headers[HeaderNames.Authorization];
            if (string.IsNullOrEmpty(authorization))
            {
                return AuthenticateResult.NoResult();
            }

            if (!authorization.StartsWith("SS14Auth "))
            {
                return AuthenticateResult.NoResult();
            }

            static bool TryParseTokenFromHeader(string authHeader, out SessionToken token)
            {
                // Have to use a static local method to work around Span<T> being unusable inside async methods.
                var tokenString = authHeader.AsSpan(9);
                return SessionToken.TryFromBase64(tokenString, out token);
            }

            if (!TryParseTokenFromHeader(authorization, out var token))
            {
                return AuthenticateResult.Fail("Token has invalid length");
            }

            var user = await _sessionManager.GetUserForSession(token);
            if (user == null)
            {
                return AuthenticateResult.Fail("Invalid token");
            }

            var principal = await _signInManager.CreateUserPrincipalAsync(user);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
    }
}