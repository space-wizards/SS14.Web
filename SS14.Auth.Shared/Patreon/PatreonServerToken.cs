using System;
using System.Security.Cryptography;
using System.Text;

namespace SS14.Auth.Shared.Patreon;

/// <summary>
/// Tokens for a campaign's game servers to query its memberships. Only the hash is stored.
/// </summary>
public static class PatreonServerToken
{
    public static string Generate()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    public static string Hash(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
