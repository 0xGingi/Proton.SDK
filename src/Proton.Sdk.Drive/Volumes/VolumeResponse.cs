namespace Proton.Sdk.Drive.Volumes;

internal sealed class VolumeResponse : ApiResponse
{
    public required VolumeDto Volume { get; init; }
}
