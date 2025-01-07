using System.Text.Json;
using System.Text.Json.Nodes;
using Proton.Sdk.Drive.Instrumentation.Metrics;
using Proton.Sdk.Drive.Serialization;
using Proton.Sdk.Instrumentation.Metrics;

namespace Proton.Sdk.Drive.Instrumentation;

internal sealed class DownloadAttemptRetryMonitor(Meter meter)
{
    private readonly AttemptRetryMonitor<InstrumentKey> _retryMonitor = new(
        meter.CreateCounter("drive_download_success_rate_total", 1, GetJsonNode(DownloadSuccessRateMetricLabels.FirstAttemptSuccessesLabel)),
        meter.CreateCounter("drive_download_success_rate_total", 1, GetJsonNode(DownloadSuccessRateMetricLabels.FirstAttemptFailuresLabel)),
        meter.CreateCounter("drive_download_success_rate_total", 1, GetJsonNode(DownloadSuccessRateMetricLabels.RetriedSuccessesLabel)),
        meter.CreateCounter("drive_download_success_rate_total", 1, GetJsonNode(DownloadSuccessRateMetricLabels.RetriedFailuresLabel)));

    public void IncrementSuccess(VolumeId volumeId, LinkId nodeId)
    {
        _retryMonitor.IncrementSuccess(new InstrumentKey(volumeId.Value, nodeId.Value));
    }

    public void IncrementFailure(VolumeId volumeId, LinkId nodeId)
    {
        _retryMonitor.IncrementFailure(new InstrumentKey(volumeId.Value, nodeId.Value));
    }

    private static JsonNode GetJsonNode(DownloadSuccessRateMetricLabels labels)
    {
        return JsonSerializer.SerializeToNode(
                labels,
                ProtonDriveInstrumentationSerializerContext.Default.DownloadSuccessRateMetricLabels)
            ?? throw new JsonException($"{typeof(DownloadSuccessRateMetricLabels)} cannot be serialized");
    }

    private readonly record struct InstrumentKey(string VolumeId, string NodeId);
}
