namespace Proton.Sdk.Drive.Instrumentation.Metrics;

internal sealed record UploadSuccessRateMetricLabels : SuccessRateMetricLabelsBase
{
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
