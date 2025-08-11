#nullable enable
using System.ComponentModel.DataAnnotations;

namespace SS14.Web.Helpers;

public class MustBeTrueAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        return value is bool b && b;
    }
}