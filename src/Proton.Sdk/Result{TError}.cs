using System.Diagnostics.CodeAnalysis;

namespace Proton.Sdk;

public readonly struct Result<TError>
{
    private readonly TError? _error;

    public Result()
    {
        IsSuccess = true;
        _error = default;
    }

    private Result(TError error)
    {
        IsSuccess = false;
        _error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public static implicit operator Result<TError>(TError error) => new(error);

    public static implicit operator Result<TError>(bool success)
        => success ? new Result<TError>() : throw new InvalidOperationException("Only \"true\" is supported");

    public bool TryGetError([MaybeNullWhen(true)] out TError error)
    {
        error = _error;
        return IsFailure;
    }
}
