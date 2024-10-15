using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Volumes;

internal sealed class VolumeDto
{
    [JsonPropertyName("VolumeID")]
    public required string Id { get; set; }

    public long? MaxSpace { get; init; }

    public required long UsedSpace { get; init; }

    public required VolumeState State { get; init; }

    [JsonPropertyName("Share")]
    public required VolumeRoot Root { get; init; }
}
