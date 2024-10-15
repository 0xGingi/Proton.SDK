using Proton.Sdk.Drive.Serialization;
using Proton.Sdk.Events;
using Proton.Sdk.Http;

namespace Proton.Sdk.Drive.Volumes;

internal readonly struct VolumesApiClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<VolumeResponse> GetVolumeAsync(string volumeId, CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.VolumeResponse)
            .GetAsync($"volumes/{volumeId}", cancellationToken).ConfigureAwait(false);
    }

    public async Task<VolumeListResponse> GetVolumesAsync(CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.VolumeListResponse)
            .GetAsync("volumes", cancellationToken).ConfigureAwait(false);
    }

    public async Task<VolumeCreationResponse> CreateVolumeAsync(VolumeCreationParameters parameters, CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.VolumeCreationResponse)
            .PostAsync("volumes", parameters, ProtonDriveApiSerializerContext.Default.VolumeCreationParameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task<LatestEventResponse> GetLatestEventAsync(VolumeId volumeId, CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.LatestEventResponse)
            .GetAsync($"volumes/{volumeId}/events/latest", cancellationToken).ConfigureAwait(false);
    }

    public async Task<VolumeEventListResponse> GetEventsAsync(VolumeId volumeId, VolumeEventId baselineEventId, CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.VolumeEventListResponse)
            .GetAsync($"volumes/{volumeId}/events/{baselineEventId}", cancellationToken).ConfigureAwait(false);
    }
}
