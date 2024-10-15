using System.Text.Json.Serialization;
using Proton.Sdk.Serialization;

namespace Proton.Sdk.Drive.Devices;

internal sealed class DeviceDeviceUpdateParameters
{
    [JsonPropertyName("SyncState")]
    [JsonConverter(typeof(BooleanToIntegerJsonConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsSynchronizationEnabled { get; set; }
}
