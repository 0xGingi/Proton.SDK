namespace Proton.Sdk.Drive;

public interface INodeIdentity
{
    LinkId NodeId { get; }
    VolumeId VolumeId { get; }
    ShareId ShareId { get; }
}
