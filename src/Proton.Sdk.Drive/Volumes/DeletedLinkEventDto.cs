namespace Proton.Sdk.Drive.Volumes;

internal sealed class DeletedLinkEventDto : EventDto
{
    public required DeletedLink Link { get; init; }
}
