using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Folders;

internal sealed class FolderCreationResponse : ApiResponse
{
    [JsonPropertyName("Folder")]
    public required FolderId FolderId { get; init; }
}
