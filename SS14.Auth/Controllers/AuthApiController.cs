using System;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using SS14.Auth.Areas.Identity.Pages.Account;
using SS14.Auth.Data;
using SS14.Auth.Sessions;

namespace SS14.Auth.Controllers
{
    [ApiController]
    [Route("/api/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly SessionManager _sessionManager;
        private readonly IEmailSender _emailSender;

        private readonly SpaceUserManager _userManager;
        private readonly SignInManager<SpaceUser> _signInManager;

        public AuthApiController(SpaceUserManager userManager, SignInManager<SpaceUser> signInManager,
            SessionManager sessionManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _sessionManager = sessionManager;
            _emailSender = emailSender;
        }

        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate(AuthenticateRequest request)
        {
            if (request.Username == null || request.Password == null)
            {
                return BadRequest();
            }

            // Console.WriteLine(Request.Headers["SS14-Launcher-Fingerprint"]);
            // Console.WriteLine(Request.Headers["User-Agent"]);

            var user = await _userManager.FindByNameOrEmailAsync(request.Username);

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
                    var token = await _sessionManager.RegisterNewSession(user, TimeSpan.FromDays(30));

                    return Ok(new AuthenticateResponse
                    {
                        Token = token.AsBase64,
                        Username = request.Username,
                        UserId = user.Id
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

            var user = new SpaceUser {UserName = userName, Email = email};
            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(p => p.Description).ToArray();
                return UnprocessableEntity(new RegisterResponseError {Errors = errors});
            }

            var confirmLink =
                await RegisterModel.GenerateEmailConfirmLink(_userManager, Url, Request, user, launcher: true);

            await RegisterModel.SendConfirmEmail(_emailSender, email, confirmLink);

            var status = _userManager.Options.SignIn.RequireConfirmedAccount
                ? RegisterResponseStatus.RegisteredNeedConfirmation
                : RegisterResponseStatus.Registered;

            return Ok(new RegisterResponse
            {
                Status = status
            });
        }

        [HttpPost("resetPassword")]
        public async Task<IActionResult> ResetPassword(RegisterRequest request)
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

            await _emailSender.SendEmailAsync(
                email,
                "Reset Password",
                "A password reset has been requested for your account.<br />" +
                $"If you did indeed request this, <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>click here</a> to reset your password.<br />" +
                "If you did not request this, simply ignore this email.");

            return Ok();
        }


        [Authorize(AuthenticationSchemes = "SS14Auth")]
        [HttpGet("ping")]
        public async Task<IActionResult> Ping()
        {
            var user = await _userManager.GetUserAsync(User);

            return Ok($"Hi, {user.UserName}");
        }
    }

    public sealed class AuthenticateRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public sealed class AuthenticateResponse
    {
        public string Token { get; set; }
        public string Username { get; set; }
        public Guid UserId { get; set; }
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


    public sealed class RegisterResponse
    {
        public RegisterResponseStatus Status { get; set; }
    }

    public sealed class RegisterResponseError
    {
        public string[] Errors { get; set; }
    }

    public enum RegisterResponseStatus
    {
        Registered,
        RegisteredNeedConfirmation
    }
}