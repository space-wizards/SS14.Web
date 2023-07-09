using System;
using SS14.Web.Helpers;

namespace SS14.Web.Pages.Shared;

public sealed record SortTabHeaderModel(ISortState State, string Column, string Text, TextAlignClass Align = TextAlignClass.Left);

public enum TextAlignClass
{
    Left,
    Right,
    Center
}

public static class TextAlignExtension
{
    public static string CssClass(this TextAlignClass textAlignClass) => textAlignClass switch
    {
        TextAlignClass.Left => "",
        TextAlignClass.Center => "text-center",
        TextAlignClass.Right => "text-right",
        _ => throw new ArgumentOutOfRangeException(nameof(TextAlignClass), "Unexpected enum value"),
    };
}