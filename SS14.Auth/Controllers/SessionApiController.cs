using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SS14.Auth.Data;

namespace SS14.Auth.Controllers
{
    [ApiController]
    [Route("/api/session")]
    public class SessionApiController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly SpaceUserManager _userManager;
        private static readonly TimeSpan SessionLength = TimeSpan.FromHours(1);

        public SessionApiController(IConfiguration configuration, SpaceUserManager userManager)
        {
            _configuration = configuration;
            _userManager = userManager;
        }

        [Authorize(AuthenticationSchemes = "SS14Auth")]
        [HttpPost("getToken")]
        public async Task<IActionResult> GetToken(GetTokenRequest request)
        {
            var user = await _userManager.GetUserAsync(User);

            var privKey = LoadPrivateKey(Convert.FromBase64String(_configuration["SessionKey:Private"]));
            var serverPkBytes = Convert.FromBase64String(request.ServerPublicKey);

            var serverRsa = RSA.Create();
            serverRsa.ImportRSAPublicKey(serverPkBytes, out _);

            var h = SHA256.Create();
            var hash = h.ComputeHash(serverPkBytes);

            var tokenHandler = new JwtSecurityTokenHandler();
            var tok = tokenHandler.CreateJwtSecurityToken(
                audience: Convert.ToBase64String(hash),
                subject: new ClaimsIdentity(new[]
                {
                    new Claim("sub", user.Id.ToString()),
                    new Claim("name", user.UserName),
                }),
                expires: DateTime.UtcNow + SessionLength,
                signingCredentials: new SigningCredentials(new ECDsaSecurityKey(privKey),
                    SecurityAlgorithms.EcdsaSha256));

            var tokStr = tokenHandler.WriteToken(tok);

            var tokBytes = Encoding.UTF8.GetBytes(tokStr);

            Console.WriteLine(tokBytes.Length);

            return Ok(Encrypt(tokBytes, serverRsa));
        }

        private static ECDsa LoadPublicKey(byte[] key)
        {
            var ecDsa = ECDsa.Create();
            ecDsa.ImportSubjectPublicKeyInfo(key, out _);
            return ecDsa;
        }

        private static ECDsa LoadPrivateKey(byte[] key)
        {
            var ecdsa = ECDsa.Create();
            ecdsa.ImportECPrivateKey(key, out _);
            return ecdsa;
        }

        // Encrypt and decrypt methods here taken from http://pages.infinit.net/ctech/20031101-0151.html.
        internal static string Encrypt(byte[] data, RSA rsa)
        {
            var sa = Aes.Create();

            var encryptor = sa.CreateEncryptor();
            var encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);

            var fmt = new RSAPKCS1KeyExchangeFormatter(rsa);
            var keyEx = fmt.CreateKeyExchange(sa.Key);

            var result = new byte[keyEx.Length + encrypted.Length + sa.IV.Length];

            keyEx.CopyTo(result.AsSpan());
            sa.IV.CopyTo(result.AsSpan(keyEx.Length));
            encrypted.CopyTo(result.AsSpan(keyEx.Length + sa.IV.Length));

            return Convert.ToBase64String(result);
        }

        // Mostly kept here for reference and testing.
        internal static byte[] Decrypt(string data, RSA rsa)
        {
            var dataBytes = Convert.FromBase64String(data);

            var sa = Aes.Create();

            var keyEx = new byte[rsa.KeySize >> 3];
            dataBytes.AsSpan(..keyEx.Length).CopyTo(keyEx);
            var def = new RSAPKCS1KeyExchangeDeformatter(rsa);
            var key = def.DecryptKeyExchange(keyEx);

            var iv = new byte[sa.IV.Length];
            var keyPlusIvLength = keyEx.Length + iv.Length;
            dataBytes.AsSpan(keyEx.Length..keyPlusIvLength).CopyTo(iv);

            var decrypt = sa.CreateDecryptor(key, iv);
            var decrypted = decrypt.TransformFinalBlock(dataBytes,
                keyEx.Length + iv.Length,
                dataBytes.Length - keyPlusIvLength);

            return decrypted;
        }

        public sealed class GetTokenRequest
        {
            public string ServerPublicKey { get; set; }
        }
    }
}