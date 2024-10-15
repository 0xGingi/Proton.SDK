namespace Proton.Sdk.Drive.Files;

internal sealed class RevisionListResponse : ApiResponse
{
    public required IReadOnlyList<RevisionDto> Revisions { get; init; }
}
