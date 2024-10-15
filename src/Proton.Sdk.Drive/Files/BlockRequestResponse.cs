using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Files;

internal sealed class BlockRequestResponse : ApiResponse
{
    [JsonPropertyName("UploadLinks")]
    public required IReadOnlyList<BlockUploadUrl> UploadUrls { get; init; }

    [JsonPropertyName("ThumbnailLink")]
    public BlockUploadUrl? ThumbnailUrl { get; init; }
}
