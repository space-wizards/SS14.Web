using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Internal;
using SS14.Auth.Shared.Data;
using SS14.Auth.Shared.Sessions;

namespace SS14.Auth.Shared.Controllers
{
    /// <summary>
    ///     Contains the API endpoints used by the launcher to log in and such.
    /// </summary>
    [ApiController]
    [Route("/api/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly SessionManager _sessionManager;
        private readonly IEmailSender _emailSender;
        private readonly ISystemClock _systemClock;

        private readonly SpaceUserManager _userManager;
        private readonly SignInManager<SpaceUser> _signInManager;

        public AuthApiController(SpaceUserManager userManager, SignInManager<SpaceUser> signInManager,
            SessionManager sessionManager, IEmailSender emailSender, ISystemClock systemClock)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _sessionManager = sessionManager;
            _emailSender = emailSender;
            _systemClock = systemClock;
        }

        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate(AuthenticateRequest request)
        {
            // Password may never be null, and only either username OR userID can be used for login, not both.
            if (request.Password == null || !(request.Username == null ^ request.UserId == null))
            {
                return BadRequest();
            }

            // Console.WriteLine(Request.Headers["SS14-Launcher-Fingerprint"]);
            // Console.WriteLine(Request.Headers["User-Agent"]);

            SpaceUser user;
            if (request.Username != null)
            {
                user = await _userManager.FindByNameOrEmailAsync(request.Username);
            }
            else
            {
                Debug.Assert(request.UserId != null);

                user = await _userManager.FindByIdAsync(request.UserId!.Value.ToString());
            }

            if (user != null)
            {
                var emailUnconfirmed = _userManager.Options.SignIn.RequireConfirmedEmail &&
                                       !await _userManager.IsEmailConfirmedAsync(user);

                if (emailUnconfirmed)
                {
                    return Unauthorized(new AuthenticateDenyResponse
                    {
                        Errors = new[]
                        {
                            "The email address for this account still needs to be confirmed. " +
                            "Please confirm your email address before trying to log in."
                        }
                    });
                }

                var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);

                if (signInResult.Succeeded)
                {
                    var (token, expireTime) = await _sessionManager.RegisterNewSession(user, SessionManager.DefaultExpireTime);

                    return Ok(new AuthenticateResponse
                    {
                        Token = token.AsBase64,
                        Username = user.UserName,
                        UserId = user.Id,
                        ExpireTime = expireTime.ToString("O")
                    });
                }
            }

            return Unauthorized(new AuthenticateDenyResponse
            {
                Errors = new[] {"Invalid login credentials."}
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            if (request.Username == null || request.Email == null)
            {
                return BadRequest();
            }

            var userName = request.Username.Trim();
            var email = request.Email.Trim();

            var user = ModelShared.CreateNewUser(userName, email, _systemClock);
            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(p => p.Description).ToArray();
                return UnprocessableEntity(new RegisterResponseError {Errors = errors});
            }

            var confirmLink =
                await ModelShared.GenerateEmailConfirmLink(_userManager, Url, Request, user, launcher: true);

            await ModelShared.SendConfirmEmail(_emailSender, email, confirmLink);

            var status = _userManager.Options.SignIn.RequireConfirmedAccount
                ? RegisterResponseStatus.RegisteredNeedConfirmation
                : RegisterResponseStatus.Registered;

            return Ok(new RegisterResponse
            {
                Status = status
            });
        }

        [HttpPost("resetPassword")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            if (request.Email == null)
            {
                return BadRequest();
            }

            var email = request.Email.Trim();

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return Ok();
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", code },
                protocol: Request.Scheme);

            await ModelShared.SendResetEmail(_emailSender, email, callbackUrl);

            return Ok();
        }

        [HttpPost("resendConfirmation")]
        public async Task<IActionResult> ResendConfirmation(ResendConfirmationRequest request)
        {
            if (request.Email == null)
            {
                return BadRequest();
            }

            var email = request.Email.Trim();

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return Ok();
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var confirmLink = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = userId, code = code },
                protocol: Request.Scheme);

            await ModelShared.SendConfirmEmail(_emailSender, email, confirmLink);

            return Ok();
        }

        [Authorize(AuthenticationSchemes = "SS14Auth")]
        [HttpGet("ping")]
        public async Task<IActionResult> Ping()
        {
            var user = await _userManager.GetUserAsync(User);

            return Ok($"Hi, {user.UserName}");
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(LogoutRequest request)
        {
            if (request.Token == null)
            {
                return BadRequest();
            }

            if (!SessionToken.TryFromBase64(request.Token, out var token))
            {
                return BadRequest();
            }

            await _sessionManager.InvalidateToken(token);
            return Ok();
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshRequest request)
        {
            if (request.Token == null)
            {
                return BadRequest();
            }

            if (!SessionToken.TryFromBase64(request.Token, out var token))
            {
                return BadRequest();
            }

            var refreshed = await _sessionManager.RefreshToken(token);
            if (refreshed == null)
            {
                return Unauthorized();
            }

            var (newToken, expireTime) = refreshed.Value;

            return Ok(new RefreshResponse
            {
                ExpireTime = expireTime.ToString("O"),
                NewToken = newToken.AsBase64
            });
        }
    }

    public sealed class AuthenticateRequest
    {
        public string Username { get; set; }
        public Guid? UserId { get; set; }
        public string Password { get; set; }
    }

    public sealed class AuthenticateResponse
    {
        public string Token { get; set; }
        public string Username { get; set; }
        public Guid UserId { get; set; }
        public string ExpireTime { get; set; }
    }

    public sealed class AuthenticateDenyResponse
    {
        public string[] Errors { get; set; } = default!;
    }

    public sealed class RegisterRequest
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public sealed class ResetPasswordRequest
    {
        public string Email { get; set; }
    }

    public sealed class ResendConfirmationRequest
    {
        public string Email { get; set; }
    }

    public sealed class RegisterResponse
    {
        public RegisterResponseStatus Status { get; set; }
    }

    public sealed class RegisterResponseError
    {
        public string[] Errors { get; set; }
    }

    public sealed class LogoutRequest
    {
        public string Token { get; set; }
    }

    public sealed class RefreshRequest
    {
        public string Token { get; set; }
    }

    public sealed class RefreshResponse
    {
        public string NewToken { get; set; }
        public string ExpireTime { get; set; }
    }

    public enum RegisterResponseStatus
    {
        Registered,
        RegisteredNeedConfirmation
    }
}