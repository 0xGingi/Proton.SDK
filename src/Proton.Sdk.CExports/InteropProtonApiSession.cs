using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Proton.Cryptography.Pgp;

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
    private static int NativeBegin(
        nint unused,
        InteropArray sessionBeginRequestBytes,
        InteropAsyncCallback callback)
    {
        try
        {
            return callback.InvokeFor(ct => InteropBeginAsync(sessionBeginRequestBytes, ct));
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "session_resume", CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe int NativeResume(
        InteropArray sessionResumeRequestBytes,
        nint* sessionHandle)
    {
        try
        {
            var sessionResumeRequest = SessionResumeRequest.Parser.ParseFrom(sessionResumeRequestBytes.AsReadOnlySpan());

            var session = ProtonApiSession.Resume(sessionResumeRequest);

            *sessionHandle = GCHandle.ToIntPtr(GCHandle.Alloc(session));

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "session_renew", CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe int NativeRenew(
        nint oldSessionHandle,
        InteropArray sessionRenewRequestBytes,
        nint* newSessionHandle)
    {
        try
        {
            var sessionRenewRequest = SessionRenewRequest.Parser.ParseFrom(sessionRenewRequestBytes.AsReadOnlySpan());

            if (!TryGetFromHandle(oldSessionHandle, out var expiredSession))
            {
                return -1;
            }

            var session = ProtonApiSession.Renew(
                expiredSession,
                sessionRenewRequest);

            *newSessionHandle = GCHandle.ToIntPtr(GCHandle.Alloc(session));

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "session_add_user_key", CallConvs = [typeof(CallConvCdecl)])]
    private static int NativeAddUserKey(nint sessionHandle, InteropArray userKeyData)
    {
        var userKey = UserKey.Parser.ParseFrom(userKeyData.AsReadOnlySpan());

        if (!TryGetFromHandle(sessionHandle, out var session))
        {
            return -1;
        }

        session.AddUserKey(session.UserId, new UserKeyId(userKey.KeyId), userKey.KeyData.Span);

        return 0;
    }

    [UnmanagedCallersOnly(EntryPoint = "session_add_armored_locked_user_key", CallConvs = [typeof(CallConvCdecl)])]
    private static int NativeAddArmoredUserKey(nint sessionHandle, InteropArray armoredUserKeyData)
    {
        var armoredUserKey = ArmoredUserKey.Parser.ParseFrom(armoredUserKeyData.AsReadOnlySpan());

        if (!TryGetFromHandle(sessionHandle, out var session))
        {
            return -1;
        }

        using var userKey = PgpPrivateKey.ImportAndUnlock(
            armoredUserKey.ArmoredKeyData.ToByteArray(),
            Encoding.UTF8.GetBytes(armoredUserKey.Passphrase),
            PgpEncoding.AsciiArmor);

        session.AddUserKey(session.UserId, new UserKeyId(armoredUserKey.KeyId), userKey.ToBytes());

        return 0;
    }

    [UnmanagedCallersOnly(EntryPoint = "session_end", CallConvs = [typeof(CallConvCdecl), typeof(CallConvMemberFunction)])]
    private static int NativeEnd(nint sessionHandle, InteropAsyncCallback callback)
    {
        try
        {
            if (!TryGetFromHandle(sessionHandle, out var session))
            {
                return -1;
            }

            callback.InvokeFor(_ => InteropEndAsync(session));

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "session_free", CallConvs = [typeof(CallConvCdecl)])]
    private static void NativeFree(nint handle)
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

    private static async ValueTask<Result<InteropArray, InteropArray>> InteropBeginAsync(
        InteropArray sessionBeginRequestBytes,
        CancellationToken cancellationToken)
    {
        try
        {
            var sessionBeginRequest = SessionBeginRequest.Parser.ParseFrom(sessionBeginRequestBytes.AsReadOnlySpan());

            var session = await ProtonApiSession.BeginAsync(sessionBeginRequest, cancellationToken).ConfigureAwait(false);

            var handle = GCHandle.ToIntPtr(GCHandle.Alloc(session));
            return ResultExtensions.Success(new IntResponse { Value = handle });
        }
        catch (Exception e)
        {
            return ResultExtensions.Failure(e);
        }
    }

    private static async ValueTask<Result<InteropArray, InteropArray>> InteropEndAsync(ProtonApiSession session)
    {
        try
        {
            await session.EndAsync().ConfigureAwait(false);

            return ResultExtensions.Success();
        }
        catch (Exception e)
        {
            return ResultExtensions.Failure(e);
        }
    }
}
