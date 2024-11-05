using System.Text.Json.Nodes;

namespace Proton.Sdk.Instrumentation.Provider;

internal sealed record ObservabilityMetricPropertiesDto(int Value, JsonNode Labels);
