using Proton.Sdk.Http;
using Proton.Sdk.Serialization;

namespace Proton.Sdk.Keys;

internal readonly struct KeysApiClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<AddressPublicKeyListResponse> GetActivePublicKeysAsync(string emailAddress, CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonCoreApiSerializerContext.Default.AddressPublicKeyListResponse)
            .GetAsync($"core/v4/keys/all?InternalOnly=1&Email={emailAddress}", cancellationToken).ConfigureAwait(false);
    }

    public async Task<KeySaltListResponse> GetKeySaltsAsync(CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonCoreApiSerializerContext.Default.KeySaltListResponse)
            .GetAsync("core/v4/keys/salts", cancellationToken).ConfigureAwait(false);
    }
}
