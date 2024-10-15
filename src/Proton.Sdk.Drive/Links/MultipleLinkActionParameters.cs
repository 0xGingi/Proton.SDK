using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Links;

public readonly struct MultipleLinkActionParameters
{
    [JsonPropertyName("LinkIDs")]
    public required IEnumerable<string> LinkIds { get; init; }
}
