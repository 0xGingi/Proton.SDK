using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Files;

internal sealed class ThumbnailBlockUploadTarget : BlockUploadTarget
{
    [JsonPropertyName("ThumbnailType")]
    public required ThumbnailType Type { get; set; }
}
