using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Instrumentation.Metrics;

internal sealed record UploadSuccessRateMetricLabels
{
    public required string Status { get; init; }

    [JsonPropertyName("retry")]
    public required string IsRetry { get; init; }

    public string ShareType { get; } = "main";

    public string Initiator { get; } = "background";

    internal static UploadSuccessRateMetricLabels FirstAttemptSuccessesLabel => new()
    {
        Status = "success",
        IsRetry = "false",
    };

    internal static UploadSuccessRateMetricLabels FirstAttemptFailuresLabel => new()
    {
        Status = "failure",
        IsRetry = "false",
    };

    internal static UploadSuccessRateMetricLabels RetriedSuccessesLabel => new()
    {
        Status = "success",
        IsRetry = "true",
    };

    internal static UploadSuccessRateMetricLabels RetriedFailuresLabel => new()
    {
        Status = "failure",
        IsRetry = "true",
    };
}
