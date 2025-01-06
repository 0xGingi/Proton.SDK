using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Files;

public struct RevisionCreationParameters
{
    [JsonPropertyName("CurrentRevisionID")]
    public string? CurrentRevisionId { get; set; }

    [JsonPropertyName("ClientUID")]
    public string? ClientId { get; set; }
}
