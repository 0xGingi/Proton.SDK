using Proton.Sdk.Drive.Serialization;
using Proton.Sdk.Http;

namespace Proton.Sdk.Drive.Devices;

internal readonly struct DevicesApiClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<DeviceListResponse> GetDevicesAsync(CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.DeviceListResponse)
            .GetAsync("/devices", cancellationToken).ConfigureAwait(false);
    }

    public async Task<DeviceCreationResponse> CreateAsync(DeviceCreationParameters parameters, CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.DeviceCreationResponse)
            .PostAsync("/devices", parameters, ProtonDriveApiSerializerContext.Default.DeviceCreationParameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ApiResponse> UpdateAsync(DeviceId id, DeviceUpdateParameters parameters, CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.ApiResponse)
            .PutAsync($"/devices/{id}", parameters, ProtonDriveApiSerializerContext.Default.DeviceUpdateParameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ApiResponse> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.ApiResponse)
            .DeleteAsync($"/devices/{id}", cancellationToken).ConfigureAwait(false);
    }
}
