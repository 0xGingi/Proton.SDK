using System.Text.Json.Serialization;
using Proton.Sdk.Cryptography;
using Proton.Sdk.Serialization;

namespace Proton.Sdk.Drive.Links;

internal sealed class Link
{
    [JsonPropertyName("LinkID")]
    public required string Id { get; init; }

    [JsonPropertyName("ParentLinkID")]
    public string? ParentId { get; init; }

    [JsonPropertyName("VolumeID")]
    public required string VolumeId { get; init; }

    public required LinkType Type { get; init; }

    public required PgpArmoredMessage Name { get; init; }

    [JsonPropertyName("NameSignatureEmail")]
    public string? NameSignatureEmailAddress { get; init; }

    [JsonPropertyName("Hash")]
    public required ReadOnlyMemory<byte> NameHashDigest { get; init; }

    public required LinkState State { get; init; }

    public required long TotalSize { get; init; }

    [JsonPropertyName("MIMEType")]
    public string? MediaType { get; init; }

    public required int Attributes { get; init; }

    [JsonPropertyName("NodeKey")]
    public required PgpArmoredPrivateKey Key { get; init; }

    [JsonPropertyName("NodePassphrase")]
    public required PgpArmoredMessage Passphrase { get; init; }

    [JsonPropertyName("NodePassphraseSignature")]
    public PgpArmoredSignature? PassphraseSignature { get; init; }

    [JsonPropertyName("SignatureEmail")]
    public string? SignatureEmailAddress { get; init; }

    [JsonPropertyName("CreateTime")]
    [JsonConverter(typeof(EpochSecondsJsonConverter))]
    public required DateTime CreationTime { get; init; }

    [JsonPropertyName("ModifyTime")]
    [JsonConverter(typeof(EpochSecondsJsonConverter))]
    public required DateTime ModificationTime { get; init; }

    [JsonPropertyName("Trashed")]
    [JsonConverter(typeof(EpochSecondsJsonConverter))]
    public DateTime? TrashTime { get; init; }

    public FileProperties? FileProperties { get; init; }
    public FolderProperties? FolderProperties { get; init; }

    public LinkSharingDetails? SharingDetails { get; init; }

    // This should really be at the revision level
    [JsonPropertyName("XAttr")]
    public PgpArmoredMessage? ExtendedAttributes { get; init; }
}
