﻿using Proton.Sdk.Http;
using Proton.Sdk.Serialization;

namespace Proton.Sdk.Addresses;

internal readonly struct AddressesApiClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<AddressListResponse> GetAddressesAsync(CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonCoreApiSerializerContext.Default.AddressListResponse)
            .GetAsync("core/v4/addresses", cancellationToken).ConfigureAwait(false);
    }

    public async Task<AddressResponse> GetAddressAsync(AddressId id, CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonCoreApiSerializerContext.Default.AddressResponse)
            .GetAsync($"core/v4/addresses/{id}", cancellationToken).ConfigureAwait(false);
    }
}
