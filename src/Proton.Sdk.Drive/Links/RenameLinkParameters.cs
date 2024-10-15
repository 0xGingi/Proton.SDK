using System.Text.Json.Serialization;
using Proton.Sdk.Cryptography;

namespace Proton.Sdk.Drive.Links;

internal sealed class RenameLinkParameters
{
    public required PgpArmoredMessage Name { get; init; }

    [JsonPropertyName("Hash")]
    public required ReadOnlyMemory<byte> NameHashDigest { get; init; }

    [JsonPropertyName("NameSignatureEmail")]
    public required string NameSignatureEmailAddress { get; init; }

    [JsonPropertyName("MIMEType")]
    public required string MediaType { get; set; }

    [JsonPropertyName("OriginalHash")]
    public required ReadOnlyMemory<byte> OriginalNameHashDigest { get; init; }
}
