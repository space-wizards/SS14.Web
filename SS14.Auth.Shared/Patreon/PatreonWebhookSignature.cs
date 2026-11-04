#nullable enable
using System;
using System.Security.Cryptography;
using System.Text;

namespace SS14.Auth.Shared.Patreon;

public static class PatreonWebhookSignature
{
    public static bool Verify(ReadOnlySpan<byte> body, string secret, string? signature)
    {
        if (string.IsNullOrEmpty(signature) || signature.Length % 2 != 0)
            return false;

        byte[] sigBytes;
        try
        {
            sigBytes = Utility.FromHex(signature);
        }
        catch (FormatException)
        {
            return false;
        }

        var hash = HMACMD5.HashData(Encoding.UTF8.GetBytes(secret), body);
        return Utility.SecretEqual(sigBytes, hash);
    }
}
