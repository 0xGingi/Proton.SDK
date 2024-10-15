using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Files;

internal sealed class RevisionCreationConflict
{
    [JsonPropertyName("ConflictLinkID")]
    public required string LinkId { get; init; }

    [JsonPropertyName("ConflictRevisionID")]
    public required string RevisionId { get; init; }

    [JsonPropertyName("ConflictDraftRevisionID")]
    public required string DraftRevisionId { get; init; }

    [JsonPropertyName("ConflictDraftClientUID")]
    public string? ClientId { get; init; }
}
