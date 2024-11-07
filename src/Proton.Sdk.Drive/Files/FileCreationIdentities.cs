using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Files;

internal sealed class FileCreationIdentities
{
    [JsonPropertyName("ID")]
    public required string LinkId { get; init; }

    [JsonPropertyName("RevisionID")]
    public required string RevisionId { get; init; }
}
