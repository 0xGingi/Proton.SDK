using System.Text.Json.Serialization;
using Proton.Sdk.Cryptography;
using Proton.Sdk.Drive.Links;
using Proton.Sdk.Serialization;

namespace Proton.Sdk.Drive.Shares;

internal sealed class ShareResponse : ApiResponse
{
    [JsonPropertyName("ShareID")]
    public required string Id { get; init; }

    [JsonPropertyName("VolumeID")]
    public required string VolumeId { get; init; }

    public required ShareType Type { get; init; }

    public required ShareState State { get; init; }

    [JsonPropertyName("Creator")]
    public required string CreatorEmailAddress { get; init; }

    [JsonPropertyName("Locked")]
    public bool IsLocked { get; init; }

    [JsonPropertyName("CreateTime")]
    [JsonConverter(typeof(EpochSecondsJsonConverter))]
    public DateTime? CreationTime { get; init; }

    [JsonPropertyName("ModifyTime")]
    [JsonConverter(typeof(EpochSecondsJsonConverter))]
    public DateTime? ModificationTime { get; init; }

    [JsonPropertyName("LinkID")]
    public required string RootLinkId { get; init; }

    [JsonPropertyName("LinkType")]
    public required LinkType RootLinkType { get; init; }

    public required PgpArmoredPrivateKey Key { get; init; }

    [JsonPropertyName("Passphrase")]
    public required PgpArmoredMessage Passphrase { get; init; }

    [JsonPropertyName("PassphraseSignature")]
    public PgpArmoredSignature? PassphraseSignature { get; init; }

    [JsonPropertyName("AddressID")]
    public required string AddressId { get; init; }

    [Obsolete]
    [JsonPropertyName("AddressKeyID")]
    public string? AddressKeyId { get; init; }

    public required IReadOnlyList<ShareMembership> Memberships { get; init; }
}
