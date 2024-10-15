namespace Proton.Sdk.CExports;

internal static class InteropAsyncCallbackExtensions
{
    public static unsafe void InvokeFor(this InteropAsyncCallbackNoCancellation callback, Func<ValueTask<Result<SdkError>>> asyncFunction)
    {
        Use(
            () => callback.OnSuccess(callback.State),
            error => callback.OnFailure(callback.State, InteropSdkError.FromManaged(error)),
            asyncFunction);
    }

    public static unsafe int InvokeFor<T>(this InteropAsyncCallback<T> callback, Func<CancellationToken, ValueTask<Result<T, SdkError>>> asyncFunction)
    {
        if (!InteropCancellationTokenSource.TryGetTokenFromHandle(callback.CancellationTokenSourceHandle, out var cancellationToken))
        {
            return -1;
        }

        Use(
            value => callback.OnSuccess(callback.State, value),
            error => callback.OnFailure(callback.State, InteropSdkError.FromManaged(error)),
            asyncFunction,
            cancellationToken);

        return 0;
    }

    private static async void Use(Action onSuccess, Action<SdkError> onFailure, Func<ValueTask<Result<SdkError>>> asyncFunction)
    {
        var result = await asyncFunction.Invoke().ConfigureAwait(false);

        if (result.TryGetError(out var error))
        {
            onFailure(error);
            return;
        }

        onSuccess();
    }

    private static async void Use<T>(
        Action<T> onSuccess,
        Action<SdkError> onFailure,
        Func<CancellationToken, ValueTask<Result<T, SdkError>>> asyncFunction,
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
