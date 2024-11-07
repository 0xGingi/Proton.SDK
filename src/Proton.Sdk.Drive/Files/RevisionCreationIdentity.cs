using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Files;

internal sealed class RevisionCreationIdentity
{
    [JsonPropertyName("ID")]
    public required string RevisionId { get; init; }
}
