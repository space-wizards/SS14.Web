#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;

#nullable enable

namespace SS14.Web.Helpers;

public interface ISortState
{
    string OrderStringForColumnButton(string column);
    IDictionary<string, string?> AllRouteData { get; }
}

public static class SortState
{
    // This method allows us to make a SortState<T> for anonymous types used in EFCore queries,
    // so that complex joined queries can use SortState<T>.
    // ReSharper disable once UnusedParameter.Global
    public static SortState<T> Build<T>(IQueryable<T> _)
    {
        return new();
    }
}

public sealed class SortState<T> : ISortState
{
    public string? CurColumn;
    public string? DefaultColumn;
    public SortOrder CurOrder = SortOrder.Ascending;
    private readonly Dictionary<string, ColumnReg> _columns = new();

    public void Init(string? reqOrder, IDictionary<string, string?> allRouteData)
    {
        if (DefaultColumn == null)
            throw new InvalidOperationException("No default column set!");

        if (reqOrder != null)
        {
            if (reqOrder.EndsWith("_desc"))
            {
                CurOrder = SortOrder.Descending;
                reqOrder = reqOrder[..^5];
            }

            CurColumn = reqOrder;
        }
        else
        {
            CurColumn = DefaultColumn;

            CurOrder = _columns[DefaultColumn].SortDefault!.Value;
        }

        AllRouteData = allRouteData;
        allRouteData.Add("sort", reqOrder);
    }

    public string OrderStringForColumnButton(string column)
    {
        var colReg = _columns[column];
        // If current column, flip order around.
        if (column == CurColumn)
            return StringWithOrder(Flip(CurOrder));

        // If default column, select default column order.
        // Otherwise ascending order.
        return StringWithOrder(colReg.SortDefault is { } def ? def :  SortOrder.Ascending);

        string StringWithOrder(SortOrder order)
        {
            // If default order return "" for string so URL is cleaner.
            if (colReg.SortDefault != null && colReg.SortDefault == order)
                return "";

            return order == SortOrder.Descending ? $"{column}_desc" : column;
        }
    }

    private static SortOrder Flip(SortOrder order)
    {
        return order switch
        {
            SortOrder.Ascending => SortOrder.Descending,
            _ => SortOrder.Ascending
        };
    }

    public IDictionary<string, string?> AllRouteData { get; private set; } = ImmutableDictionary<string, string?>.Empty;

    public void AddColumn<TKey>(string name, Expression<Func<T, TKey>> expr, SortOrder? sortDefault = null)
    {
        _columns.Add(name, new ColumnReg<TKey>(expr, sortDefault));

        if (sortDefault != null)
            DefaultColumn = name;
    }

    public IOrderedQueryable<T> ApplyToQuery(IQueryable<T> query)
    {
        var col = _columns[CurColumn!];
        return col.ApplyToQuery(query, CurOrder);
    }

    private abstract record ColumnReg(SortOrder? SortDefault)
    {
        public abstract IOrderedQueryable<T> ApplyToQuery(IQueryable<T> query, SortOrder order);
    }

    private sealed record ColumnReg<TKey>(Expression<Func<T, TKey>> Expr, SortOrder? SortDefault) : ColumnReg(SortDefault)
    {
        public override IOrderedQueryable<T> ApplyToQuery(IQueryable<T> query, SortOrder order)
        {
            return order switch
            {
                SortOrder.Ascending => query.OrderBy(Expr),
                SortOrder.Descending => query.OrderByDescending(Expr),
                _ => throw new InvalidOperationException()
            };
        }
    }
}

public enum SortOrder
{
    Ascending,
    Descending
}