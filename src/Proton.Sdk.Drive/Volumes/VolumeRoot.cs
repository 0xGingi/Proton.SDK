using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Volumes;

internal sealed class VolumeRoot
{
    [JsonPropertyName("ShareID")]
    public required string ShareId { get; init; }

    [JsonPropertyName("LinkID")]
    public required string LinkId { get; init; }
}
