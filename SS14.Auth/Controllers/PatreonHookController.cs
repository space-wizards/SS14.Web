using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SS14.Auth.Shared;
using SS14.Auth.Shared.Config;
using SS14.Auth.Shared.Data;

namespace SS14.Auth.Controllers
{
    [ApiController]
    [Route("/hook/patreon")]
    public class PatreonHookController : ControllerBase
    {
        private readonly IOptions<PatreonConfiguration> _patreonCfg;
        private readonly ILogger<PatreonHookController> _logger;
        private readonly ApplicationDbContext _db;

        public PatreonHookController(
            IOptions<PatreonConfiguration> patreonCfg, 
            ILogger<PatreonHookController> logger,
            ApplicationDbContext db)
        {
            _patreonCfg = patreonCfg;
            _logger = logger;
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> Hook(
            [FromHeader(Name = "X-Patreon-Signature")] string signature,
            [FromHeader(Name = "X-Patreon-Event")] string trigger)
        {
            _logger.LogDebug("Handling Patreon event of type {Event} signature {Signature}", trigger, signature);
            
            var ms = new MemoryStream();
            await Request.Body.CopyToAsync(ms);
            ms.Position = 0;

            var sigBytes = Utility.FromHex(signature);
            var hmac = new HMACMD5(Encoding.UTF8.GetBytes(_patreonCfg.Value.WebhookSecret));
            // ReSharper disable once MethodHasAsyncOverload
            hmac.ComputeHash(ms);
            ms.Position = 0;

            if (!Utility.SecretEqual(sigBytes, hmac.Hash))
            {
                _logger.LogDebug("Signature failed to validate, ignoring");
                return Unauthorized();
            }

            _logger.LogDebug("Patreon webhook passed signature check!");

            if (_patreonCfg.Value.LogWebhooks)
            {
                _logger.LogTrace("Storing in database");
                using var reader = new StreamReader(ms);
                // ReSharper disable once MethodHasAsyncOverload
                var str = reader.ReadToEnd();

                // ReSharper disable once MethodHasAsyncOverload
                _db.PatreonWebhookLogs.Add(new PatreonWebhookLog
                {
                    Content = str,
                    Time = DateTimeOffset.UtcNow,
                    Trigger = trigger
                });

                await _db.SaveChangesAsync();
            }
            
            return Ok();
        }
    }
}