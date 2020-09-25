using System;

#nullable enable

namespace SS14.Auth.Shared.Sessions
{
    public readonly struct SessionToken
    {
        public const int TokenLength = 32;

        public readonly byte[] Token;

        public SessionToken(byte[] token)
        {
            if (token.Length != TokenLength)
            {
                throw new ArgumentException(nameof(token));
            }

            Token = token;
        }

        public static bool TryFromBase64(ReadOnlySpan<char> base64, out SessionToken token)
        {
            var data = new byte[TokenLength];
            Convert.TryFromBase64Chars(base64, data, out var written);

            if (written != TokenLength)
            {
                token = default;
                return false;
            }

            token = new SessionToken(data);
            return true;
        }

        public string AsBase64 => Convert.ToBase64String(Token);
    }
}