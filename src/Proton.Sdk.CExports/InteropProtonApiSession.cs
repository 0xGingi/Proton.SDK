using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Proton.Cryptography.Pgp;
using Proton.Sdk.Authentication;

namespace Proton.Sdk.CExports;

internal static class InteropProtonApiSession
{
    internal static bool TryGetFromHandle(nint handle, [MaybeNullWhen(false)] out ProtonApiSession session)
    {
        var gcHandle = GCHandle.FromIntPtr(handle);

        session = gcHandle.Target as ProtonApiSession;

        return session is not null;
    }

    [UnmanagedCallersOnly(EntryPoint = "session_begin", CallConvs = [typeof(CallConvCdecl)])]
    private static nint Begin(
        InteropArray username,
        InteropArray password,
        InteropProtonClientOptions interopOptions,
        InteropAsyncCallback<nint> callback)
    {
        try
        {
            return callback.InvokeFor(ct => BeginAsync(username.Utf8ToString(), password.ToArray(), interopOptions.ToManaged(), ct));
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "session_resume", CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe int Resume(
        InteropArray id,
        InteropArray username,
        InteropArray userId,
        InteropArray accessToken,
        InteropArray refreshToken,
        InteropArray scopes,
        bool isWaitingForSecondFactorCode,
        byte passwordMode,
        InteropProtonClientOptions options,
        nint* sessionHandle)
    {
        try
        {
            var session = ProtonApiSession.Resume(
                id.Utf8ToString(),
                username.Utf8ToString(),
                new UserId(userId.Utf8ToString()),
                accessToken.Utf8ToString(),
                refreshToken.Utf8ToString(),
                scopes.Utf8ToString().Replace("[", string.Empty).Replace("]", string.Empty).Split(","),
                isWaitingForSecondFactorCode,
                (PasswordMode)passwordMode,
                options.ToManaged());

            *sessionHandle = GCHandle.ToIntPtr(GCHandle.Alloc(session));

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "session_add_user_key", CallConvs = [typeof(CallConvCdecl)])]
    private static int AddUserKey(nint sessionHandle, InteropArray keyId, InteropArray keyData)
    {
        if (!TryGetFromHandle(sessionHandle, out var session))
        {
            return -1;
        }

        session.AddUserKey(session.UserId, new UserKeyId(keyId.Utf8ToString()), keyData.AsReadOnlySpan());

        return 0;
    }

    [UnmanagedCallersOnly(EntryPoint = "add_armored_locked_user_key", CallConvs = [typeof(CallConvCdecl)])]
    private static int AddUserKey(nint sessionHandle, InteropArray keyId, InteropArray keyData, InteropArray passphrase)
    {
        if (!TryGetFromHandle(sessionHandle, out var session))
        {
            return -1;
        }

        using var userKey = PgpPrivateKey.ImportAndUnlock(keyData.AsReadOnlySpan(), passphrase.AsReadOnlySpan(), PgpEncoding.AsciiArmor);

        session.AddUserKey(session.UserId, new UserKeyId(keyId.Utf8ToString()), userKey.ToBytes());

        return 0;
    }

    [UnmanagedCallersOnly(EntryPoint = "session_end", CallConvs = [typeof(CallConvCdecl), typeof(CallConvMemberFunction)])]
    private static int End(nint sessionHandle, InteropAsyncCallbackNoCancellation callback)
    {
        try
        {
            if (!TryGetFromHandle(sessionHandle, out var session))
            {
                return -1;
            }

            callback.InvokeFor(() => EndAsync(session));

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "session_free", CallConvs = [typeof(CallConvCdecl)])]
    private static void Free(nint handle)
    {
        try
        {
            var gcHandle = GCHandle.FromIntPtr(handle);

            gcHandle.Free();
        }
        catch
        {
            // Ignore
        }
    }

    private static async ValueTask<Result<nint, SdkError>> BeginAsync(
        string username,
        ReadOnlyMemory<byte> password,
        ProtonClientOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ProtonApiSession.BeginAsync(username, password, options, cancellationToken).ConfigureAwait(false);

            return GCHandle.ToIntPtr(GCHandle.Alloc(session));
        }
        catch (Exception e)
        {
            return new SdkError(-1, e.Message);
        }
    }

    private static async ValueTask<Result<SdkError>> EndAsync(ProtonApiSession session)
    {
        try
        {
            await session.EndAsync().ConfigureAwait(false);

            return true;
        }
        catch (Exception e)
        {
            return new SdkError(-1, e.Message);
        }
    }
}
