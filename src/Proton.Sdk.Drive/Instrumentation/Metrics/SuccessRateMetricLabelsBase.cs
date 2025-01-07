using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Instrumentation.Metrics;

internal abstract record SuccessRateMetricLabelsBase
{
    public required string Status { get; init; }

    [JsonPropertyName("retry")]
    public required string IsRetry { get; init; }

    public string ShareType { get; } = "main";
}
