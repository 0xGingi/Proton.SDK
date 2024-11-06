using System.Text.Json.Serialization;

namespace Proton.Sdk.Instrumentation.Observability;

internal record ObservabilityMetric(
    string Name,
    int Version,
    long Timestamp,
    [property: JsonPropertyName("Data")] ObservabilityMetricProperties Properties);
