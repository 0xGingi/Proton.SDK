using System.Runtime.InteropServices;
using Proton.Sdk.CExports;

namespace Proton.Sdk.Drive.CExports;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct InteropNodeIdentity
{
    public readonly InteropArray VolumeId;
    public readonly InteropArray Id;

    public INodeIdentity ToManaged()
    {
        return new NodeIdentity
        {
            VolumeId = new VolumeId(VolumeId.Utf8ToString()),
            Id = new LinkId(Id.Utf8ToString()),
        };
    }

    private sealed class NodeIdentity : INodeIdentity
    {
        public required VolumeId VolumeId { get; init; }

        public required LinkId Id { get; init; }
    }
}
