using Proton.Sdk.Http;
using Proton.Sdk.Serialization;

namespace Proton.Sdk.Events;

internal readonly struct EventsApiClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<LatestEventResponse> GetLatestEventAsync(CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonCoreApiSerializerContext.Default.LatestEventResponse)
            .GetAsync("core/v6/events/latest", cancellationToken).ConfigureAwait(false);
    }

    public async Task<EventListResponse> GetEventsAsync(EventId baselineEventId, CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonCoreApiSerializerContext.Default.EventListResponse)
            .GetAsync($"core/v6/events/{baselineEventId}", cancellationToken).ConfigureAwait(false);
    }
}
