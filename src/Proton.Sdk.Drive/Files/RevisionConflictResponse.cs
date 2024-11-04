using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Files;

internal sealed class RevisionConflictResponse : ApiResponse
{
    [JsonPropertyName("Details")]
    public required RevisionConflict Conflict { get; init; }
}
