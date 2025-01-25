using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Files;

internal sealed class RevisionConflict
{
    [JsonPropertyName("ConflictLinkID")]
    public string? LinkId { get; init; }

    [JsonPropertyName("ConflictRevisionID")]
    public string? RevisionId { get; init; }

    [JsonPropertyName("ConflictDraftRevisionID")]
    public string? DraftRevisionId { get; init; }

    [JsonPropertyName("ConflictDraftClientUID")]
    public string? DraftClientId { get; init; }
}
