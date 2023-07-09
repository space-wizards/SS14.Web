using System.Collections.Generic;

namespace SS14.Web.Helpers;

public static class CollectionHelper
{
    public static Dictionary<TKey, TValue> ShallowClone<TKey, TValue>(this IDictionary<TKey, TValue> dict)
        where TKey : notnull
    {
        return new(dict);
    }
}