using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Files;

public sealed class Block
{
    public required int Index { get; init; }

    [JsonPropertyName("URL")]
    public required string Url { get; init; }

    [JsonPropertyName("EncSignature")]
    public required string EncryptedSignature { get; init; }

    [JsonPropertyName("SignatureEmail")]
    public required string SignatureEmailAddress { get; init; }
}
