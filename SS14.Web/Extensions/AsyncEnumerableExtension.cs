#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SS14.Web.Extensions;

public static class AsyncEnumerableExtension
{
    public static async ValueTask<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> enumerable, CancellationToken ct = default)
    {
        var source = enumerable.WithCancellation(ct);
        List<T> list = [];
        await foreach (var item in source)
        {
            list.Add(item);
        }

        return list;
    }
}
