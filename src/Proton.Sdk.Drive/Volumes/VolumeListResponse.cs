namespace Proton.Sdk.Drive.Volumes;

internal sealed class VolumeListResponse : ApiResponse
{
    public required IReadOnlyList<VolumeDto> Volumes { get; init; }
}
