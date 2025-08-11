#nullable enable
using System;
using System.Linq.Expressions;

namespace SS14.Web.Helpers;

public static class SearchHelper
{
    public static void CombineSearch<T>(
        ref Expression<Func<T, bool>> a,
        Expression<Func<T, bool>> b)
    {
        var param = Expression.Parameter(typeof(T));
        var body = Expression.Or(Expression.Invoke(a, param), Expression.Invoke(b, param));
        a = Expression.Lambda<Func<T, bool>>(body, param);
    }
}