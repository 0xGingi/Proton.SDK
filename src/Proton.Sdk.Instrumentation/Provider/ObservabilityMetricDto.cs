using System.Text.Json.Serialization;

namespace Proton.Sdk.Instrumentation.Provider;

internal record ObservabilityMetricDto(
    string Name,
    int Version,
    long Timestamp,
    [property: JsonPropertyName("Data")] ObservabilityMetricPropertiesDto PropertiesDto);
