using System.Text.Json.Serialization;
using Proton.Sdk.Cryptography;
using Proton.Sdk.Drive.Links;

namespace Proton.Sdk.Drive.Files;

internal sealed class FileCreationParameters : NodeCreationParameters
{
    [JsonPropertyName("MIMEType")]
    public required string MediaType { get; init; }

    public required ReadOnlyMemory<byte> ContentKeyPacket { get; init; }

    public required PgpArmoredSignature ContentKeyPacketSignature { get; init; }

    [JsonPropertyName("ClientUID")]
    public string? ClientId { get; init; }

    public long? IntendedUploadSize { get; init; }
}
