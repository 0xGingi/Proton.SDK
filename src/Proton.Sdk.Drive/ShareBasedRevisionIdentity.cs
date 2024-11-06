namespace Proton.Sdk.Drive;

public partial class ShareBasedRevisionIdentity
{
    public ShareBasedRevisionIdentity(
        ShareId shareId,
        LinkId nodeId,
        RevisionId revisionId)
    {
        ShareId = shareId;
        NodeId = nodeId;
        RevisionId = revisionId;
    }
}
