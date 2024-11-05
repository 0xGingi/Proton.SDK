namespace Proton.Sdk.Instrumentation.Provider;

internal sealed class ObservabilityMetricsParameters(IReadOnlyList<ObservabilityMetricDto> metrics)
{
    public IReadOnlyList<ObservabilityMetricDto> Metrics { get; } = metrics;
}
