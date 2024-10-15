using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Files;

internal sealed record RevisionIdentity
{
    [JsonPropertyName("ID")]
    public required string LinkId { get; init; }

    [JsonPropertyName("RevisionID")]
    public required string RevisionId { get; init; }
}
