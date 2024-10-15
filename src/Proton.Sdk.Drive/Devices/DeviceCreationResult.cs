using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Devices;

internal sealed record DeviceCreationResult
{
    [JsonPropertyName("DeviceID")]
    public required string DeviceId { get; init; }

    [JsonPropertyName("ShareID")]
    public required string ShareId { get; init; }

    [JsonPropertyName("LinkID")]
    public required string LinkId { get; init; }
}
