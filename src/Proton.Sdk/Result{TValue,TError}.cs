using System.Diagnostics.CodeAnalysis;

namespace Proton.Sdk;

public readonly struct Result<TValue, TError>
{
    private readonly TValue? _value;
    private readonly TError? _error;

    private Result(TValue value)
    {
        IsSuccess = true;
        _value = value;
        _error = default;
    }

    private Result(TError error)
    {
        IsSuccess = false;
        _value = default;
        _error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public static implicit operator Result<TValue, TError>(TValue value) => new(value);
    public static implicit operator Result<TValue, TError>(TError error) => new(error);

    public bool TryGetValue([MaybeNullWhen(false)] out TValue value, [MaybeNullWhen(true)] out TError error)
    {
        value = _value;
        error = _error;
        return IsSuccess;
    }

    public bool TryGetError([MaybeNullWhen(false)] out TError error, [MaybeNullWhen(true)] out TValue value)
    {
        value = _value;
        error = _error;
        return IsFailure;
    }
}
