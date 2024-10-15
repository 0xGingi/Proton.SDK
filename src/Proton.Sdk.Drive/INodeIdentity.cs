namespace Proton.Sdk.Drive;

public interface INodeIdentity
{
    VolumeId VolumeId { get; }
    LinkId Id { get; }
}
