using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Files;

internal sealed class FileCreationResponse : ApiResponse
{
    [JsonPropertyName("File")]
    public required FileCreationIdentities Identities { get; init; }
}
