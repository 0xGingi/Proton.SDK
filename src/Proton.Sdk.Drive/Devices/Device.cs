using System.Text.Json.Serialization;
using Proton.Sdk.Serialization;

namespace Proton.Sdk.Drive.Devices;

internal sealed record Device
{
    [JsonPropertyName("DeviceID")]
    public required string Id { get; init; }

    [JsonPropertyName("VolumeID")]
    public required string VolumeId { get; init; }

    [JsonPropertyName("Type")]
    public required DevicePlatform Platform { get; init; }

    [JsonPropertyName("SyncState")]
    [JsonConverter(typeof(BooleanToIntegerJsonConverter))]
    public required bool SynchronizationIsEnabled { get; init; }
}
