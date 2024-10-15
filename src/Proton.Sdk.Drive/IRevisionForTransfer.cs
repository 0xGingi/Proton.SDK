namespace Proton.Sdk.Drive;

public interface IRevisionForTransfer
{
    RevisionId Id { get; }
    RevisionState State { get; }
    ReadOnlyMemory<byte>? ManifestSignature { get; }
    string? SignatureEmailAddress { get; }
    IReadOnlyList<ReadOnlyMemory<byte>> SamplesSha256Digests { get; }
}
