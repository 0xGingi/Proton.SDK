using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Proton.Cryptography.Pgp;
using Proton.Sdk.CExports.Logging;
using Proton.Sdk.Cryptography;

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
    private static unsafe int NativeBegin(
        nint unused,
        InteropArray sessionBeginRequestBytes,
        InteropRequestResponseBodyCallback requestResponseBodyCallback,
        InteropSecretRequestedCallback secretRequestedCallback,
        InteropAsyncCallback callback)
    {
        try
        {
            var onSecretRequested = new Func<KeyCacheMissMessage, bool>(
                keyCacheMissMessage => secretRequestedCallback.OnSecretRequested(
                    secretRequestedCallback.State,
                    InteropArray.FromMemory(keyCacheMissMessage.ToByteArray())));

            return callback.InvokeFor(ct => InteropBeginAsync(sessionBeginRequestBytes, requestResponseBodyCallback, onSecretRequested, ct));
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "session_resume", CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe int NativeResume(
        InteropArray sessionResumeRequestBytes,
        InteropRequestResponseBodyCallback requestResponseBodyCallback,
        InteropSecretRequestedCallback secretRequestedCallback,
        nint* sessionHandle)
    {
        try
        {
            var onSecretRequested = new Func<KeyCacheMissMessage, bool>(
                keyCacheMissMessage => secretRequestedCallback.OnSecretRequested(
                    secretRequestedCallback.State,
                    InteropArray.FromMemory(keyCacheMissMessage.ToByteArray())));

            var sessionResumeRequest = SessionResumeRequest.Parser.ParseFrom(sessionResumeRequestBytes.AsReadOnlySpan());

            if (InteropLoggerProvider.TryGetFromHandle((nint)sessionResumeRequest.Options.LoggerProviderHandle, out var loggerProvider))
            {
                sessionResumeRequest.Options.LoggerFactory = new LoggerFactory([loggerProvider]);
            }

            sessionResumeRequest.Options.CustomHttpMessageHandlerFactory = () => ResponsePassingHttpHandler.Create(requestResponseBodyCallback);

            sessionResumeRequest.Options.SecretsCache = new InteropFallbackSecretsCacheDecorator(
                new InMemorySecretsCache(),
                key => onSecretRequested.Invoke(key.ToCacheMissMessage()),
                sessionResumeRequest.Options.LoggerFactory);

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

    [UnmanagedCallersOnly(EntryPoint = "session_register_armored_locked_user_key", CallConvs = [typeof(CallConvCdecl)])]
    private static int NativeRegisterArmoredUserKey(nint sessionHandle, InteropArray armoredUserKeyData)
    {
        try
        {
            if (!TryGetFromHandle(sessionHandle, out var session))
            {
                return -1;
            }

            var armoredUserKey = ArmoredUserKey.Parser.ParseFrom(armoredUserKeyData.AsReadOnlySpan());

            using var userKey = PgpPrivateKey.ImportAndUnlock(
                armoredUserKey.ArmoredKeyData.ToByteArray(),
                Encoding.UTF8.GetBytes(armoredUserKey.Passphrase),
                PgpEncoding.AsciiArmor);

            AddUserKeyToCache(session, new UserKeyId(armoredUserKey.KeyId), userKey.ToBytes());

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "session_register_address_keys", CallConvs = [typeof(CallConvCdecl)])]
    private static int NativeRegisterAddressKeys(nint sessionHandle, InteropArray requestBytes)
    {
        try
        {
            if (!TryGetFromHandle(sessionHandle, out var session))
            {
                return -1;
            }

            var request = AddressKeyRegistrationRequest.Parser.ParseFrom(requestBytes.AsReadOnlySpan());

            var cacheKeys = new List<CacheKey>(request.Keys.Count);

            foreach (var addressKey in request.Keys)
            {
                var cacheKey = Address.GetAddressKeyCacheKey(addressKey.AddressKeyId);
                session.SecretsCache.Set(cacheKey, addressKey.RawUnlockedData.Span, addressKey.IsPrimary ? (byte)1 : (byte)0);
                cacheKeys.Add(cacheKey);
            }

            session.SecretsCache.IncludeInGroup(Address.GetAddressKeyGroupCacheKey(request.AddressId), CollectionsMarshal.AsSpan(cacheKeys));

            return 0;
        }
        catch
        {
            return -1;
        }
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
        InteropRequestResponseBodyCallback requestResponseBodyCallback,
        Func<KeyCacheMissMessage, bool> onSecretRequested,
        CancellationToken cancellationToken)
    {
        try
        {
            var sessionBeginRequest = SessionBeginRequest.Parser.ParseFrom(sessionBeginRequestBytes.AsReadOnlySpan());

            if (InteropLoggerProvider.TryGetFromHandle((nint)sessionBeginRequest.Options.LoggerProviderHandle, out var loggerProvider))
            {
                sessionBeginRequest.Options.LoggerFactory = new LoggerFactory([loggerProvider]);
            }

            sessionBeginRequest.Options.CustomHttpMessageHandlerFactory = () => ResponsePassingHttpHandler.Create(requestResponseBodyCallback);

            sessionBeginRequest.Options.SecretsCache = new InteropFallbackSecretsCacheDecorator(
                new InMemorySecretsCache(),
                key => onSecretRequested.Invoke(key.ToCacheMissMessage()),
                sessionBeginRequest.Options.LoggerFactory);

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

    private static void AddUserKeyToCache(ProtonApiSession session, UserKeyId keyId, ReadOnlySpan<byte> keyData)
    {
        var cacheKey = ProtonAccountClient.GetUserKeyCacheKey(keyId);
        session.SecretsCache.Set(cacheKey, keyData, 1);
        var cacheKeys = new List<CacheKey>(1) { cacheKey };
        session.SecretsCache.IncludeInGroup(ProtonAccountClient.GetUserKeyGroupCacheKey(session.UserId), CollectionsMarshal.AsSpan(cacheKeys));
    }
}
