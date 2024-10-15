namespace Proton.Sdk.Drive;

public interface INodeForRename : INodeIdentity
{
    ReadOnlyMemory<byte> NameHashDigest { get; }
}
