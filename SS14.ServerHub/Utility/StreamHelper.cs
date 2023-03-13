using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SS14.ServerHub.Utility;

public static class StreamHelper
{
    public static async ValueTask<bool> CopyToLimitedAsync(Stream from, Stream to, int limit, CancellationToken cancel)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(Math.Min(4096, limit));
        var total = 0;
        while (true)
        {
            var read = await from.ReadAsync(buffer.AsMemory(), cancel);
            if (read == 0)
                return true;

            if (read + total > limit)
                return false;

            await to.WriteAsync(buffer.AsMemory(0, read), cancel);
            total += read;
        }
    }
}