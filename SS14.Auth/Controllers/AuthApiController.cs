using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
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

        private readonly UserManager<SpaceUser> _userManager;
        private readonly SignInManager<SpaceUser> _signInManager;

        public AuthApiController(UserManager<SpaceUser> userManager, SignInManager<SpaceUser> signInManager,
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
            // Console.WriteLine(Request.Headers["SS14-Launcher-Fingerprint"]);
            // Console.WriteLine(Request.Headers["User-Agent"]);

            var user = await _userManager.FindByNameAsync(request.Username);

            if (user == null)
            {
                return Unauthorized();
            }

            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);

            if (!signInResult.Succeeded)
            {
                return Unauthorized();
            }

            var token = await _sessionManager.RegisterNewSession(user, TimeSpan.FromDays(30));

            return Ok(new AuthenticateResponse
            {
                Token = token.AsBase64,
                Username = request.Username,
                UserId = user.Id
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
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

    public sealed class RegisterRequest
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
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