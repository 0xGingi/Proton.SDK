using System.Text.Json.Serialization;
using Proton.Sdk.Serialization;

namespace Proton.Sdk.Drive.Devices;

internal sealed class DeviceUpdateParameters
{
    public required DeviceDeviceUpdateParameters Device { get; init; }

    internal sealed class DeviceDeviceUpdateParameters
    {
        [JsonPropertyName("SyncState")]
        [JsonConverter(typeof(BooleanToIntegerJsonConverter))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? SynchronizationIsEnabled { get; init; }

        [JsonConverter(typeof(EpochSecondsJsonConverter))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? LastSyncTime { get; init; }
    }
}
