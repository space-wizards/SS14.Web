using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SS14.Web.Helpers;

public static class FormHiddenRoutes
{
    /// <summary>
    /// Helper function that will copy route parameters (such as sort state) into hidden fields of a form.
    /// This avoids having to copy them manually or something like a search form resetting sort order.
    /// </summary>
    /// <param name="routeData"></param>
    /// <param name="exclude"></param>
    /// <returns></returns>
    public static IHtmlContent Make(Dictionary<string, string> routeData, params string[] exclude)
    {
        var builder = new HtmlContentBuilder();
        foreach (var (k, v) in routeData)
        {
            if (exclude.Contains(k) || v == null)
                continue;

            var tag = new TagBuilder("input");
            tag.Attributes.Add("type", "hidden");
            tag.Attributes.Add("name", k);
            tag.Attributes.Add("value", v);

            // ReSharper disable once MustUseReturnValue
            builder.AppendHtml(tag);
        }

        return builder;
    }
}