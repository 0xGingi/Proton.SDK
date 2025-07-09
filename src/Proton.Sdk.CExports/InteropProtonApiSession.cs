using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Proton.Cryptography.Pgp;
using Proton.Sdk.CExports.Logging;
using Proton.Sdk.Cryptography;

namespace Proton.Sdk.CExports;

internal static class InteropProtonApiSession
{
    internal static bool TryGetFromHandle(nint handle, [MaybeNullWhen(false)] out ProtonApiSession session)
    {
        if (handle == 0)
        {
            session = null;
            return false;
        }

        var gcHandle = GCHandle.FromIntPtr(handle);

        session = gcHandle.Target as ProtonApiSession;

        return session is not null;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct InteropTwoFactorRequestedCallback
    {
        public nint State;
        // essentially output1 is the 2fa, second one is the data password.
        public delegate* unmanaged[Cdecl]<nint, InteropArray, out InteropArray, out InteropArray, bool> Callback;
    }

    [UnmanagedCallersOnly(EntryPoint = "session_begin", CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe int NativeBegin(
        nint unused,
        InteropArray sessionBeginRequestBytes,
        InteropRequestResponseBodyCallback requestResponseBodyCallback,
        InteropSecretRequestedCallback secretRequestedCallback,
        InteropTwoFactorRequestedCallback twoFactorRequestedCallback,
        InteropTokensRefreshedCallback tokensRefreshedCallback,
        InteropAsyncCallback callback)
    {
        try
        {
            var onSecretRequested = secretRequestedCallback.OnSecretRequested != null && secretRequestedCallback.State != null
                ? new Func<KeyCacheMissMessage, bool>(keyCacheMissMessage =>
                {
                    Console.WriteLine("Secret requested ping");
                    var messageBytes = InteropArray.FromMemory(keyCacheMissMessage.ToByteArray());
                    try
                    {
                        return secretRequestedCallback.OnSecretRequested(secretRequestedCallback.State, messageBytes);
                    }
                    finally
                    {
                        messageBytes.Free();
                    }
                })
                : null;

            var onTwoFactorRequested = twoFactorRequestedCallback.Callback != null && twoFactorRequestedCallback.State != nint.Zero
                ? new Func<KeyCacheMissMessage, (string? TwoFactor, string? DataPassword)>(keyCacheMissMessage =>
                {
                    Console.WriteLine("Two factor callback ping");
                    var contextBytes = InteropArray.FromMemory(keyCacheMissMessage.ToByteArray());
                    InteropArray outCode, outDataPassword;
                    var result = twoFactorRequestedCallback.Callback(
                        twoFactorRequestedCallback.State, contextBytes, out outCode, out outDataPassword);
                    contextBytes.Free();
                    if (result)
                    {
                        var twoFactor = StringResponse.Parser.ParseFrom(outCode.AsReadOnlySpan()).Value;
                        var dataPassword = StringResponse.Parser.ParseFrom(outDataPassword.AsReadOnlySpan()).Value;
                        outCode.Free();
                        outDataPassword.Free();
                        return (twoFactor, dataPassword);
                    }

                    return (null, null);
                })
                : null;

            return callback.InvokeFor(
                ct => InteropBeginAsync(sessionBeginRequestBytes, requestResponseBodyCallback, onSecretRequested, onTwoFactorRequested, tokensRefreshedCallback, ct));
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
        InteropTokensRefreshedCallback tokensRefreshedCallback,
        nint* sessionHandle)
    {
        try
        {
            var onSecretRequested = secretRequestedCallback.OnSecretRequested != null && secretRequestedCallback.State != null
                ? new Func<KeyCacheMissMessage, bool>(keyCacheMissMessage =>
                {
                    var messageBytes = InteropArray.FromMemory(keyCacheMissMessage.ToByteArray());

                    try
                    {
                        return secretRequestedCallback.OnSecretRequested(secretRequestedCallback.State, messageBytes);
                    }
                    finally
                    {
                        messageBytes.Free();
                    }
                })
                : null;

            var sessionResumeRequest = SessionResumeRequest.Parser.ParseFrom(sessionResumeRequestBytes.AsReadOnlySpan());

            if (InteropLoggerProvider.TryGetFromHandle((nint)sessionResumeRequest.Options.LoggerProviderHandle, out var loggerProvider))
            {
                sessionResumeRequest.Options.LoggerFactory = new LoggerFactory([loggerProvider]);
            }

            sessionResumeRequest.Options.CustomHttpMessageHandlerFactory = () => ResponsePassingHttpHandler.Create(
                requestResponseBodyCallback);

            var cacheLogger = sessionResumeRequest.Options.LoggerFactory?.CreateLogger<InMemorySecretsCache>() ?? NullLogger<InMemorySecretsCache>.Instance;
            sessionResumeRequest.Options.SecretsCache = onSecretRequested is not null
                ? new InteropFallbackSecretsCacheDecorator(
                    new InMemorySecretsCache(cacheLogger),
                    key => onSecretRequested.Invoke(key.ToCacheMissMessage()),
                    sessionResumeRequest.Options.LoggerFactory)
                : new InMemorySecretsCache(cacheLogger);

            sessionResumeRequest.Options.BindingsLanguage = "C";

            var session = ProtonApiSession.Resume(
                sessionResumeRequest);

            session.TokenCredential.TokensRefreshed += (accessToken, refreshToken) =>
                tokensRefreshedCallback.TokensRefreshed(accessToken, refreshToken);

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
        InteropTokensRefreshedCallback tokensRefreshedCallback,
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

            session.TokenCredential.TokensRefreshed += (accessToken, refreshToken) =>
                tokensRefreshedCallback.TokensRefreshed(accessToken, refreshToken);

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

            if (gcHandle.Target is not ProtonApiSession)
            {
                return;
            }

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
        Func<KeyCacheMissMessage, bool>? onSecretRequested,
        Func<KeyCacheMissMessage, (string? TwoFactor, string? DataPassword)>? onTwoFactorRequested,
        InteropTokensRefreshedCallback tokensRefreshedCallback,
        CancellationToken cancellationToken)
    {
        try
        {
            var sessionBeginRequest = SessionBeginRequest.Parser.ParseFrom(sessionBeginRequestBytes.AsReadOnlySpan());

            if (sessionBeginRequest.Options.HasLoggerProviderHandle
                && InteropLoggerProvider.TryGetFromHandle((nint)sessionBeginRequest.Options.LoggerProviderHandle, out var loggerProvider))
            {
                sessionBeginRequest.Options.LoggerFactory = new LoggerFactory([loggerProvider]);
            }

            sessionBeginRequest.Options.CustomHttpMessageHandlerFactory = () => ResponsePassingHttpHandler.Create(requestResponseBodyCallback);

            var cacheLogger = sessionBeginRequest.Options.LoggerFactory?.CreateLogger<InMemorySecretsCache>() ?? NullLogger<InMemorySecretsCache>.Instance;
            sessionBeginRequest.Options.SecretsCache = onSecretRequested is not null ?
                new InteropFallbackSecretsCacheDecorator(
                    new InMemorySecretsCache(cacheLogger),
                    key => onSecretRequested.Invoke(key.ToCacheMissMessage()),
                    sessionBeginRequest.Options.LoggerFactory)
                : new InMemorySecretsCache(cacheLogger);

            sessionBeginRequest.Options.BindingsLanguage = "C";

            var session = await ProtonApiSession.BeginAsync(
                sessionBeginRequest,
                cancellationToken).ConfigureAwait(false);

            session.LoggerFactory.CreateLogger<ProtonApiSession>().LogInformation("Session created: {UserId}", session.UserId);

            // 2FA callback logic
            if (session.IsWaitingForSecondFactorCode && onTwoFactorRequested != null)
            {
                var twoFactorRequest = new KeyCacheMissMessage
                {
                    HolderId = session.UserId.Value,
                    HolderName = session.Username,
                    ValueName = "two_factor_code"
                };
                var (code, dataPassword) = onTwoFactorRequested(twoFactorRequest);
                if (!string.IsNullOrEmpty(code))
                {
                    await session.ApplySecondFactorCodeAsync(code, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    Console.WriteLine("2FA code is empty or null");
                }

                if (!string.IsNullOrEmpty(dataPassword))
                {
                    await session.ApplyDataPasswordAsync(Encoding.UTF8.GetBytes(dataPassword), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    Console.WriteLine("Data password is empty or null");
                }
            }

            session.TokenCredential.TokensRefreshed += (accessToken, refreshToken) =>
                tokensRefreshedCallback.TokensRefreshed(accessToken, refreshToken);

            var handle = GCHandle.ToIntPtr(GCHandle.Alloc(session));
            return ResultExtensions.Success(new IntResponse { Value = handle });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Session begin caught an exception: {e}");
            return ResultExtensions.Failure(e, InteropErrorConverter.SetDomainAndCodes);
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
            return ResultExtensions.Failure(e, InteropErrorConverter.SetDomainAndCodes);
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
