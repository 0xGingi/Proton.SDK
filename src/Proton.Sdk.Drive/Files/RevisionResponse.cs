namespace Proton.Sdk.Drive.Files;

internal sealed class RevisionResponse : ApiResponse
{
    public required RevisionWithBlockPage Revision { get; init; }
}
