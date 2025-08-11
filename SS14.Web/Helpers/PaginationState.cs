#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

#nullable enable

namespace SS14.Web.Helpers;

public interface IPaginationState
{
    public IDictionary<string, string?> AllRouteData { get; }
    bool HasNextPage { get; }
    bool HasPrevPage { get; }
    int PageIndex { get; }
    int PerPage { get; }
    int TotalCount { get; }
    int DefaultPerPage { get; }
}

public class PaginationState<T> : IPaginationState
{
    public int DefaultPerPage { get; }
    public int PageIndex { get; private set; }
    public int PerPage { get; private set; }
    public int TotalCount => List.TotalCount;
    public PaginatedList<T> List = default!;
    public bool HasPrevPage => List.HasPrevPage;
    public bool HasNextPage => List.HasNextPage;
    public T[] Items => List.PaginatedItems;

    public IDictionary<string, string?> AllRouteData { get; private set; } =
        ImmutableDictionary<string, string?>.Empty;

    public PaginationState(int defaultPerPage)
    {
        PerPage = DefaultPerPage = defaultPerPage;
    }

    public void Init(int? pageIndex, int? perPage, Dictionary<string, string?> allRouteData)
    {
        if (pageIndex != null)
            PageIndex = pageIndex.Value;

        if (perPage != null)
            PerPage = perPage.Value;

        AllRouteData = allRouteData;
        if (PerPage != DefaultPerPage)
            AllRouteData.Add("perPage", PerPage.ToString(CultureInfo.InvariantCulture));
    }

    public async Task LoadAsync(IQueryable<T> query, int? count = null)
    {
        List = await PaginatedList<T>.CreateAsync(query, PageIndex, PerPage, count);
    }

    public async Task LoadLinqAsync<TQuery>(
        IQueryable<TQuery> query,
        Func<IEnumerable<TQuery>, IEnumerable<T>> convert)
    {
        List = await PaginatedList<T>.CreateLinqAsync(query, convert, PageIndex, PerPage);
    }
}