namespace Proton.Sdk.Drive.Instrumentation.Metrics;

internal sealed record DownloadSuccessRateMetricLabels : SuccessRateMetricLabelsBase
{
    internal static DownloadSuccessRateMetricLabels FirstAttemptSuccessesLabel => new()
    {
        Status = "success",
        IsRetry = "false",
    };

    internal static DownloadSuccessRateMetricLabels FirstAttemptFailuresLabel => new()
    {
        Status = "failure",
        IsRetry = "false",
    };

    internal static DownloadSuccessRateMetricLabels RetriedSuccessesLabel => new()
    {
        Status = "success",
        IsRetry = "true",
    };

    internal static DownloadSuccessRateMetricLabels RetriedFailuresLabel => new()
    {
        Status = "failure",
        IsRetry = "true",
    };
}
