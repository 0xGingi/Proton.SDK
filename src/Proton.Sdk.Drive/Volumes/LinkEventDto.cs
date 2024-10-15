using System.Text.Json.Serialization;
using Proton.Sdk.Drive.Links;

namespace Proton.Sdk.Drive.Volumes;

internal sealed class LinkEventDto : EventDto
{
    [JsonPropertyName("ContextShareID")]
    public required string ContextShareId { get; init; }

    public required Link Link { get; init; }
}
