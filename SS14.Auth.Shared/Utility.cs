using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace SS14.Auth.Shared
{
    public static class Utility
    {
        // Taken from https://github.com/aspnet/AspLabs/blob/41de6d7a808742c5db72f537018dee85bcdafca8/src/WebHooks/src/Microsoft.AspNetCore.WebHooks.Receivers/Filters/WebHookVerifySignatureFilter.cs#L158
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static bool SecretEqual(ReadOnlySpan<byte> inputA, ReadOnlySpan<byte> inputB)
        {
            if (inputA.Length != inputB.Length)
            {
                return false;
            }

            var areSame = true;
            for (var i = 0; i < inputA.Length; i++)
            {
                areSame &= inputA[i] == inputB[i];
            }

            return areSame;
        }

        public static byte[] FromHex(ReadOnlySpan<char> hex)
        {
            var arr = new byte[hex.Length >> 1];

            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = byte.Parse(hex.Slice(i << 1, 2), NumberStyles.HexNumber);
            }

            return arr;
        }
    }
}