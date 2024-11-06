using Proton.Sdk.Http;

namespace Proton.Sdk.Instrumentation.Observability;

internal readonly struct ObservabilityApiClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<ApiResponse> SendMetricsAsync(ObservabilityMetricsParameters metricsParameters, CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonInstrumentationApiSerializerContext.Default.ApiResponse)
            .PostAsync("/v1/metrics", metricsParameters, ProtonInstrumentationApiSerializerContext.Default.ObservabilityMetricsParameters, cancellationToken)
            .ConfigureAwait(false);
    }
}
