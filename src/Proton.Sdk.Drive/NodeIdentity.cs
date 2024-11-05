namespace Proton.Sdk.Drive;

public sealed partial class NodeIdentity : INodeIdentity
{
    public NodeIdentity(ShareId shareId, VolumeId volumeId, LinkId linkId)
    {
        ShareId = shareId;
        VolumeId = volumeId;
        NodeId = linkId;
    }

    public NodeIdentity(ShareId shareId, INodeIdentity nodeIdentity)
    {
        ShareId = shareId;
        VolumeId = nodeIdentity.VolumeId;
        NodeId = nodeIdentity.NodeId;
    }
}
