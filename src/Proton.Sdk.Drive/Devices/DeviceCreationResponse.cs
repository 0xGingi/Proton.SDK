using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Devices;

internal sealed class DeviceCreationResponse : ApiResponse
{
    [JsonPropertyName("Device")]
    public required DeviceCreationResult Result { get; init; }
}
