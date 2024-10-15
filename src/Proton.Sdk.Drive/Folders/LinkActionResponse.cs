using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Folders;

internal sealed class LinkActionResponse
{
    [JsonPropertyName("LinkID")]
    public required string LinkId { get; init; }

    public required ApiResponse Response { get; init; }
}
