#nullable enable
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace SS14.Web.Models.Types;

public record Result<TResult, TError>(TResult? Value, TError? Error) where TResult : class where TError : class
{
    [PublicAPI]
    [MemberNotNullWhen(false, nameof(Error))]
    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsSuccess => Error == null;

    public bool TryGetResult([NotNullWhen(true)] out TResult? result)
    {
        result = Value;
        return IsSuccess;
    }

    public static Result<TResult, TError> Success(TResult value)
    {
        return new Result<TResult, TError>(value, null);
    }

    public static Result<TResult, TError> Failure(TError error)
    {
        return new Result<TResult, TError>(null, error);
    }
}

/// <summary>
/// Used to signify that a result does not return any data, but was successful.
/// </summary>
public sealed record Void
{
    [PublicAPI]
    public static Void Nothing = new Void();
}
