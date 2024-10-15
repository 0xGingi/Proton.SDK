using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Links;

internal readonly struct LinkSharingDetails
{
    [JsonPropertyName("ShareID")]
    public required string ShareId { get; init; }
}
