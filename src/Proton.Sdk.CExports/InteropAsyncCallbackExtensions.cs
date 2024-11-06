namespace Proton.Sdk.CExports;

internal static class InteropAsyncCallbackExtensions
{
    public static unsafe int InvokeFor(this InteropAsyncCallback callback, Func<CancellationToken, ValueTask<Result<InteropArray, InteropArray>>> asyncFunction)
    {
        if (!InteropCancellationTokenSource.TryGetTokenFromHandle(callback.CancellationTokenSourceHandle, out var cancellationToken))
        {
            return -1;
        }

        Use(
            value => callback.OnSuccess(callback.State, value),
            error => callback.OnFailure(callback.State, error),
            asyncFunction,
            cancellationToken);

        return 0;
    }

    private static async void Use<T>(
        Action<T> onSuccess,
        Action<T> onFailure,
        Func<CancellationToken, ValueTask<Result<T, T>>> asyncFunction,
        CancellationToken cancellationToken)
    {
        var result = await asyncFunction.Invoke(cancellationToken).ConfigureAwait(false);

        if (result.TryGetError(out var error, out var value))
        {
            onFailure(error);
            return;
        }

        onSuccess(value);
    }
}
