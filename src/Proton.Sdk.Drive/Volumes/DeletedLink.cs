using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Volumes;

internal sealed class DeletedLink
{
    [JsonPropertyName("LinkID")]
    public required string Id { get; init; }
}
