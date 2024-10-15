namespace Proton.Sdk.Drive.Links;

internal sealed class LinkResponse : ApiResponse
{
    public required Link Link { get; init; }
}
