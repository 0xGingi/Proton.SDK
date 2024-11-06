using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Google.Protobuf;
using Proton.Cryptography.Pgp;
using Proton.Sdk.Cryptography;
using Proton.Sdk.Drive.Files;
using Proton.Sdk.Drive.Links;
using Proton.Sdk.Drive.Serialization;

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
