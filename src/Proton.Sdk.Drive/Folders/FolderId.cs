using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Folders;

internal readonly struct FolderId
{
    [JsonPropertyName("ID")]
    public required string Value { get; init; }
}
