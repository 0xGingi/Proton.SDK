using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Files;

internal sealed class BlockUploadRequestParameters
{
    [JsonPropertyName("AddressID")]
    public required string AddressId { get; init; }

    [JsonPropertyName("ShareID")]
    public required string ShareId { get; init; }

    [JsonPropertyName("LinkID")]
    public required string LinkId { get; init; }

    [JsonPropertyName("RevisionID")]
    public required string RevisionId { get; init; }

    [JsonPropertyName("BlockList")]
    public required IReadOnlyList<BlockCreationParameters> Blocks { get; init; }

    [JsonPropertyName("ThumbnailList")]
    public required IReadOnlyList<ThumbnailCreationParameters> Thumbnails { get; init; }
}
