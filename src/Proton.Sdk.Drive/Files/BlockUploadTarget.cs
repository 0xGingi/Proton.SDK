using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Files;

internal class BlockUploadTarget
{
    [JsonPropertyName("BareURL")]
    public required string BareUrl { get; set; }

    public required string Token { get; set; }
}
