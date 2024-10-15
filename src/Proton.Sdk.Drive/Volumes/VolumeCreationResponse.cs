namespace Proton.Sdk.Drive.Volumes;

internal sealed class VolumeCreationResponse : ApiResponse
{
    public required VolumeDto Volume { get; init; }
}
