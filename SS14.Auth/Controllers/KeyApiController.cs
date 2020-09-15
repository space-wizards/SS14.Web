using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace SS14.Auth.Controllers
{
    [ApiController]
    [Route("/api/keys")]
    public sealed class KeyApiController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public KeyApiController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("sessionSign")]
        public IActionResult Foo()
        {
            return Ok(new SessionSignKeyResponse(_configuration["SessionKey:Public"], "secp256r1"));
        }
    }

    public sealed class SessionSignKeyResponse
    {
        public string Key { get; }
        public string Type { get; }

        public SessionSignKeyResponse(string key, string type)
        {
            Key = key;
            Type = type;
        }
    }
}