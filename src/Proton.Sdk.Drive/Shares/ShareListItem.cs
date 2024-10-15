using System.Text.Json.Serialization;
using Proton.Sdk.Serialization;

namespace Proton.Sdk.Drive.Shares;

public sealed class ShareListItem
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
    public bool? IsLocked { get; init; }

    [JsonPropertyName("CreateTime")]
    [JsonConverter(typeof(EpochSecondsJsonConverter))]
    public DateTime? CreationTime { get; init; }

    [JsonPropertyName("ModifyTime")]
    [JsonConverter(typeof(EpochSecondsJsonConverter))]
    public DateTime? ModificationTime { get; init; }

    [JsonPropertyName("LinkID")]
    public required string RootLinkId { get; init; }
}
