using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Files;

internal sealed class BlockUploadUrl
{
    public required string Token { get; init; }

    [JsonPropertyName("URL")]
    public required string Value { get; init; }
}
