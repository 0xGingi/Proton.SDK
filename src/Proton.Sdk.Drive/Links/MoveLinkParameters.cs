using System.Text.Json.Serialization;
using Proton.Sdk.Cryptography;

namespace Proton.Sdk.Drive.Links;

internal sealed class MoveLinkParameters
{
    [JsonPropertyName("NewShareID")]
    public string? ShareId { get; init; }

    [JsonPropertyName("ParentLinkID")]
    public required string ParentLinkId { get; init; }

    [JsonPropertyName("NodePassphrase")]
    public required PgpArmoredMessage KeyPassphrase { get; init; }

    public required PgpArmoredMessage Name { get; init; }

    [JsonPropertyName("Hash")]
    public required ReadOnlyMemory<byte> NameHashDigest { get; init; }

    [JsonPropertyName("NameSignatureEmail")]
    public required string NameSignatureEmailAddress { get; init; }

    [JsonPropertyName("OriginalHash")]
    public required ReadOnlyMemory<byte> OriginalNameHashDigest { get; init; }
}
