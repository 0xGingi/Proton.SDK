using Proton.Sdk.Http;

namespace Proton.Sdk.Instrumentation.Provider;

internal readonly struct ObservabilityApiClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<ApiResponse> SendMetricsAsync(ObservabilityMetricsParameters metricsParameters, CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonInstrumentationApiSerializerContext.Default.ApiResponse)
            .PostAsync("/data/v1/metrics", metricsParameters, ProtonInstrumentationApiSerializerContext.Default.ObservabilityMetricsParameters, cancellationToken)
            .ConfigureAwait(false);
    }
}
