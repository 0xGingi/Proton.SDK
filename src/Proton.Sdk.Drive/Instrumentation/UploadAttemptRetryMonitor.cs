using System.Text.Json;
using System.Text.Json.Nodes;
using Proton.Sdk.Drive.Instrumentation.Metrics;
using Proton.Sdk.Drive.Serialization;
using Proton.Sdk.Instrumentation.Metrics;

namespace Proton.Sdk.Drive.Instrumentation;

internal sealed class UploadAttemptRetryMonitor(Meter meter)
{
    private readonly AttemptRetryMonitor<InstrumentKey> _retryMonitor = new(
        meter.CreateCounter("drive_upload_success_rate_total", 2, GetJsonNode(UploadSuccessRateMetricLabels.FirstAttemptSuccessesLabel)),
        meter.CreateCounter("drive_upload_success_rate_total", 2, GetJsonNode(UploadSuccessRateMetricLabels.FirstAttemptFailuresLabel)),
        meter.CreateCounter("drive_upload_success_rate_total", 2, GetJsonNode(UploadSuccessRateMetricLabels.RetriedSuccessesLabel)),
        meter.CreateCounter("drive_upload_success_rate_total", 2, GetJsonNode(UploadSuccessRateMetricLabels.RetriedFailuresLabel)));

    public void IncrementSuccess(VolumeId volumeId, LinkId parentLinkId, string fileName)
    {
        _retryMonitor.IncrementSuccess(new InstrumentKey(volumeId.Value, parentLinkId.Value, fileName));
    }

    public void IncrementFailure(VolumeId volumeId, LinkId parentLinkId, string fileName)
    {
        _retryMonitor.IncrementFailure(new InstrumentKey(volumeId.Value, parentLinkId.Value, fileName));
    }

    private static JsonNode GetJsonNode(UploadSuccessRateMetricLabels labels)
    {
        return JsonSerializer.SerializeToNode(
                labels,
                ProtonDriveInstrumentationSerializerContext.Default.UploadSuccessRateMetricLabels)
            ?? throw new JsonException($"{typeof(UploadSuccessRateMetricLabels)} cannot be serialized");
    }

    private readonly record struct InstrumentKey(string VolumeId, string ParentLinkId, string FileName);
}
