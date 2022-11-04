using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SS14.Web.Areas.Admin;

public sealed class PaginatedList<T>
{
    public T[] PaginatedItems { get; }
    public int PageIndex { get; }
    public int PageCount { get; }

    public PaginatedList(T[] paginatedItems, int totalCount, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        PageCount = (int) Math.Ceiling(totalCount / (double) pageSize);
            
        PaginatedItems = paginatedItems;
    }

    public bool HasNextPage => PageIndex < PageCount - 1;
    public bool HasPrevPage => PageIndex > 0;

    public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> query, int pageIndex, int pageSize)
    {
        var count = await query.CountAsync();
        var items = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToArrayAsync();

        return new PaginatedList<T>(items, count, pageIndex, pageSize);
    }
}