using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Files;

internal sealed class BlockRequestResponse : ApiResponse
{
    [JsonPropertyName("UploadLinks")]
    public required IReadOnlyList<BlockUploadTarget> UploadTargets { get; set; }

    [JsonPropertyName("ThumbnailLinks")]
    public required IReadOnlyList<ThumbnailBlockUploadTarget> ThumbnailUploadTargets { get; set; }
}
