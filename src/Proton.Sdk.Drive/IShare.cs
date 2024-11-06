namespace Proton.Sdk.Drive;

public interface IShare : IShareForCommand
{
    VolumeId VolumeId { get; }

    ShareMetadata Metadata();
}
