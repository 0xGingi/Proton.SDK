using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Files;

internal sealed class RevisionCreationResponse : ApiResponse
{
    [JsonPropertyName("Revision")]
    public required RevisionCreationIdentity Identity { get; init; }
}
