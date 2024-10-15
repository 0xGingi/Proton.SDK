using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Files;

internal sealed class ThumbnailBlock
{
    [JsonPropertyName("ThumbnailID")]
    public required string Id { get; init; }

    [JsonPropertyName("BareURL")]
    public required string BareUrl { get; init; }

    public required string Token { get; init; }
}
