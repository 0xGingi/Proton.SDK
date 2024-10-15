using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Files;

internal sealed class FileConflictResponse : ApiResponse
{
    [JsonPropertyName("Details")]
    public required RevisionCreationConflict Conflict { get; init; }
}
