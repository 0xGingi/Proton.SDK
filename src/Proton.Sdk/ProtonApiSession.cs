using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Proton.Cryptography.Srp;
using Proton.Sdk.Authentication;
using Proton.Sdk.Cryptography;
using Proton.Sdk.Keys;

namespace Proton.Sdk;

public sealed class ProtonApiSession
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    private bool _isEnded;
    private Action? _ended;

    private ProtonApiSession(
        SessionId sessionId,
        string username,
        UserId userId,
        TokenCredential tokenCredential,
        IEnumerable<string> scopes,
        bool isWaitingForSecondFactorCode,
        PasswordMode passwordMode,
        ProtonClientConfiguration configuration,
        ILogger<ProtonApiSession> logger)
    {
        _httpClient = configuration.GetHttpClient(this);
        _logger = logger;

        Username = username;
        UserId = userId;
        SessionId = sessionId;
        TokenCredential = tokenCredential;
        Scopes = scopes.ToArray().AsReadOnly();
        IsWaitingForSecondFactorCode = isWaitingForSecondFactorCode;
        PasswordMode = passwordMode;
        Configuration = configuration;
    }

    public event Action? Ended
    {
        add
        {
            _ended += value;
            TokenCredential.RefreshTokenExpired -= OnRefreshTokenExpired;
            TokenCredential.RefreshTokenExpired += OnRefreshTokenExpired;
        }
        remove
        {
            _ended -= value;
            TokenCredential.RefreshTokenExpired -= OnRefreshTokenExpired;
        }
    }

    public SessionId SessionId { get; }

    public string Username { get; }

    public UserId UserId { get; }

    public TokenCredential TokenCredential { get; }

    public IReadOnlyList<string> Scopes { get; private set; }

    public bool IsWaitingForSecondFactorCode { get; private set; }

    public PasswordMode PasswordMode { get; }

    internal ISecretsCache SecretsCache => Configuration.SecretsCache;

    internal ILoggerFactory LoggerFactory => Configuration.LoggerFactory;

    private ProtonClientConfiguration Configuration { get; }

    private AuthenticationApiClient AuthenticationApi => new(_httpClient);
    private KeysApiClient KeysApi => new(_httpClient);

    public static async Task<ProtonApiSession> BeginAsync(
        SessionBeginRequest sessionBeginRequest,
        CancellationToken cancellationToken)
    {
        var configuration = new ProtonClientConfiguration(sessionBeginRequest.Options);

        var logger = configuration.LoggerFactory.CreateLogger<ProtonApiSession>();

        var httpClient = configuration.GetHttpClient();

        var authApiClient = new AuthenticationApiClient(httpClient);

        var sessionInitiationResponse = await authApiClient.InitiateSessionAsync(sessionBeginRequest.Username, cancellationToken)
            .ConfigureAwait(false);

        var srpClient = SrpClient.Create(
            sessionBeginRequest.Username,
            Encoding.UTF8.GetBytes(sessionBeginRequest.Password),
            sessionInitiationResponse.Salt.Span,
            sessionInitiationResponse.Modulus,
            SrpClient.GetDefaultModulusVerificationKey());

        var srpClientHandshake = srpClient.ComputeHandshake(sessionInitiationResponse.ServerEphemeral.Span, 2048);

        var authResponse = await authApiClient.AuthenticateAsync(sessionInitiationResponse, srpClientHandshake, sessionBeginRequest.Username, cancellationToken)
            .ConfigureAwait(false);

        var tokenCredential = new TokenCredential(
            new AuthenticationApiClient(httpClient),
            authResponse.SessionId,
            authResponse.AccessToken,
            authResponse.RefreshToken,
            configuration.LoggerFactory.CreateLogger<TokenCredential>());

        var session = new ProtonApiSession(
            new SessionId(authResponse.SessionId),
            sessionBeginRequest.Username,
            new UserId(authResponse.UserId),
            tokenCredential,
            authResponse.Scopes,
            authResponse.SecondFactorParameters?.IsEnabled == true,
            authResponse.PasswordMode,
            new ProtonClientConfiguration(sessionBeginRequest.Options),
            logger);

        if (session is { IsWaitingForSecondFactorCode: false, PasswordMode: PasswordMode.Single })
        {
            try
            {
                await session.ApplyDataPasswordAsync(Encoding.UTF8.GetBytes(sessionBeginRequest.Password), cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Ignore
                // TODO: log that
            }
        }

        return session;
    }

    public static ProtonApiSession Resume(SessionResumeRequest sessionResumeRequest)
    {
        var configuration = new ProtonClientConfiguration(sessionResumeRequest.Options);

        var logger = configuration.LoggerFactory.CreateLogger<ProtonApiSession>();

        var tokenCredential = new TokenCredential(
            new AuthenticationApiClient(configuration.GetHttpClient()),
            sessionResumeRequest.SessionId.Value,
            sessionResumeRequest.AccessToken,
            sessionResumeRequest.RefreshToken,
            configuration.LoggerFactory.CreateLogger<TokenCredential>());

        var session = new ProtonApiSession(
            sessionResumeRequest.SessionId,
            sessionResumeRequest.Username,
            sessionResumeRequest.UserId,
            tokenCredential,
            sessionResumeRequest.Scopes,
            sessionResumeRequest.IsWaitingForSecondFactorCode,
            sessionResumeRequest.PasswordMode,
            configuration,
            logger
        );

        logger.Log(LogLevel.Information, "Session {SessionId} was resumed", session.SessionId);

        return session;
    }

    public static ProtonApiSession Renew(
        ProtonApiSession expiredSession,
        SessionRenewRequest sessionRenewRequest)
    {
        var tokenCredential = new TokenCredential(
            new AuthenticationApiClient(expiredSession.Configuration.GetHttpClient()),
            sessionRenewRequest.SessionId.Value,
            sessionRenewRequest.AccessToken,
            sessionRenewRequest.RefreshToken,
            expiredSession.Configuration.LoggerFactory.CreateLogger<TokenCredential>());

        var logger = expiredSession.Configuration.LoggerFactory.CreateLogger<ProtonApiSession>();

        return new ProtonApiSession(
            sessionRenewRequest.SessionId,
            expiredSession.Username,
            expiredSession.UserId,
            tokenCredential,
            sessionRenewRequest.Scopes,
            sessionRenewRequest.IsWaitingForSecondFactorCode,
            sessionRenewRequest.PasswordMode,
            expiredSession.Configuration,
            logger);
    }

    public static async Task EndAsync(string id, string accessToken, ProtonClientOptions options)
    {
        var configuration = new ProtonClientConfiguration(options);

        var httpClient = configuration.GetHttpClient();

        var authenticationApiClient = new AuthenticationApiClient(httpClient);

        await authenticationApiClient.EndSessionAsync(id, accessToken).ConfigureAwait(false);
    }

    public async Task ApplySecondFactorCodeAsync(string secondFactorCode, CancellationToken cancellationToken)
    {
        var response = await AuthenticationApi.ValidateSecondFactorAsync(secondFactorCode, cancellationToken).ConfigureAwait(false);

        IsWaitingForSecondFactorCode = false;
        Scopes = response.Scopes;
    }

    public async Task ApplyDataPasswordAsync(ReadOnlyMemory<byte> password, CancellationToken cancellationToken)
    {
        var response = await KeysApi.GetKeySaltsAsync(cancellationToken).ConfigureAwait(false);

        foreach (var keySalt in response.KeySalts)
        {
            if (keySalt.Value.Length <= 0)
            {
                continue;
            }

            Configuration.SecretsCache.Set(
                GetAccountKeyPassphraseCacheKey(keySalt.KeyId),
                DeriveSecretFromPassword(password.Span, keySalt.Value.Span).Span);
        }
    }

    public async Task RefreshScopesAsync(CancellationToken cancellationToken)
    {
        var scopesResponse = await AuthenticationApi.GetScopesAsync(cancellationToken).ConfigureAwait(false);

        Scopes = scopesResponse.Scopes;
    }

    public async Task<bool> EndAsync()
    {
        if (_isEnded)
        {
            return true;
        }

        var response = await AuthenticationApi.EndSessionAsync().ConfigureAwait(false);

        if (response.IsSuccess)
        {
            _isEnded = true;

            _ended?.Invoke();
        }

        return _isEnded;
    }

    public void AddUserKey(UserId userId, UserKeyId keyId, ReadOnlySpan<byte> keyData)
    {
        var cacheKey = GetUserKeyPassphraseCacheKey(keyId);
        Configuration.SecretsCache.Set(cacheKey, keyData, 1);
        var cacheKeys = new List<CacheKey>(1) { cacheKey };
        Configuration.SecretsCache.IncludeInGroup(GetUserKeyGroupCacheKey(userId), CollectionsMarshal.AsSpan(cacheKeys));
    }

    internal static CacheKey GetUserKeyPassphraseCacheKey(UserKeyId keyId) => GetAccountKeyPassphraseCacheKey(keyId.Value);
    internal static CacheKey GetLegacyAddressKeyPassphraseCacheKey(AddressKeyId keyId) => GetAccountKeyPassphraseCacheKey(keyId.Value);

    internal HttpClient GetHttpClient(string? baseRoutePath = default, TimeSpan? attemptTimeout = default)
    {
        return baseRoutePath is null && attemptTimeout is null ? _httpClient : Configuration.GetHttpClient(this, baseRoutePath, attemptTimeout);
    }

    private static ReadOnlyMemory<byte> DeriveSecretFromPassword(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt)
    {
        var hashDigest = SrpClient.HashPassword(password, salt).AsMemory();

        // Skip the first 29 characters which include the algorithm type, the number of rounds and the salt.
        return hashDigest[29..];
    }

    private static CacheKey GetAccountKeyPassphraseCacheKey(string keyId) => new("account-key", keyId, "passphrase");
    private static CacheKey GetUserKeyGroupCacheKey(UserId id) => new("user", id.Value, "keys");

    private void OnRefreshTokenExpired()
    {
        _isEnded = true;
        _ended?.Invoke();
    }
}
