using System.Diagnostics.CodeAnalysis;

namespace Template.Application.Common;

/// <summary>Outcome of a use case: either a value or an <see cref="Common.Error"/>.</summary>
public sealed class Result<T>
{
    private Result(T value)
    {
        Value = value;
    }

    private Result(Error error)
    {
        Error = error;
    }

    public T? Value { get; }

    public Error? Error { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);
}
