namespace Proton.Sdk.Instrumentation.Observability;

internal sealed class ObservabilityMetricsParameters(IReadOnlyList<ObservabilityMetric> metrics)
{
    public IReadOnlyList<ObservabilityMetric> Metrics { get; } = metrics;
}
