using System;
using System.ComponentModel.DataAnnotations;

namespace SS14.Web.Models;

/// <summary>
/// Validates that a given value is a valid absolute URL.
/// This validation is more specific than the standard <see cref="UrlAttribute"/> while also allowing other schemes.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class SpaceUrlAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext context)
    {
        var url = value?.ToString();
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            return new ValidationResult(ErrorMessage ?? "Invalid URL");

        return ValidationResult.Success;
    }
}
