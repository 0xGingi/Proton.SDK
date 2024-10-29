using Proton.Cryptography.Srp;
using Proton.Sdk.Http;
using Proton.Sdk.Serialization;

namespace Proton.Sdk.Authentication;

internal readonly struct AuthenticationApiClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<SessionInitiationResponse> InitiateSessionAsync(string username, CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonCoreApiSerializerContext.Default.SessionInitiationResponse)
            .PostAsync(
                "auth/v4/info",
                new SessionInitiationRequest(username),
                ProtonCoreApiSerializerContext.Default.SessionInitiationRequest,
                cancellationToken).ConfigureAwait(false);
    }

    public Task<AuthenticationResponse> AuthenticateAsync(
        SessionInitiationResponse initiationResponse,
        in SrpClientHandshake srpClientHandshake,
        string username,
        CancellationToken cancellationToken)
    {
        var request = new AuthenticationRequest
        {
            ClientEphemeral = srpClientHandshake.Ephemeral,
            ClientProof = srpClientHandshake.Proof,
            SrpSessionId = initiationResponse.SrpSessionId,
            Username = username,
        };

        return AuthenticateAsync(request, cancellationToken);
    }

    public async Task<ScopesResponse> ValidateSecondFactorAsync(string secondFactorCode, CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonCoreApiSerializerContext.Default.ScopesResponse)
            .PostAsync(
                "auth/v4/2fa",
                new SecondFactorValidationRequest(secondFactorCode),
                ProtonCoreApiSerializerContext.Default.SecondFactorValidationRequest,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ApiResponse> EndSessionAsync()
    {
        return await _httpClient
            .Expecting(ProtonCoreApiSerializerContext.Default.ApiResponse)
            .DeleteAsync("auth/v4", CancellationToken.None).ConfigureAwait(false);
    }

    public async Task<ApiResponse> EndSessionAsync(string sessionId, string accessToken)
    {
        return await _httpClient
            .Expecting(ProtonCoreApiSerializerContext.Default.ApiResponse)
            .DeleteAsync("auth/v4", sessionId, accessToken, CancellationToken.None).ConfigureAwait(false);
    }

    public async Task<SessionRefreshResponse> RefreshSessionAsync(
        string sessionId,
        string accessToken,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonCoreApiSerializerContext.Default.SessionRefreshResponse)
            .PostAsync(
                "auth/v4/refresh",
                sessionId,
                accessToken,
                new SessionRefreshRequest(refreshToken),
                ProtonCoreApiSerializerContext.Default.SessionRefreshRequest,
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<ScopesResponse> GetScopesAsync(CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonCoreApiSerializerContext.Default.ScopesResponse)
            .GetAsync("auth/v4/scopes", cancellationToken).ConfigureAwait(false);
    }

    public async Task<ModulusResponse> GetRandomSrpModulusAsync(CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonCoreApiSerializerContext.Default.ModulusResponse)
            .GetAsync("auth/v4/modulus", cancellationToken).ConfigureAwait(false);
    }

    private async Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest request, CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonCoreApiSerializerContext.Default.AuthenticationResponse)
            .PostAsync("auth/v4", request, ProtonCoreApiSerializerContext.Default.AuthenticationRequest, cancellationToken).ConfigureAwait(false);
    }
}
