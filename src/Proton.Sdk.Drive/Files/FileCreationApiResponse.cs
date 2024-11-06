using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Files;

internal sealed class FileCreationApiResponse : ApiResponse
{
    [JsonPropertyName("File")]
    public required RevisionIdentity RevisionIdentity { get; init; }
}
