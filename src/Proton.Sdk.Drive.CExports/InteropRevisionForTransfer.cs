using System.Runtime.InteropServices;
using Proton.Sdk.CExports;

namespace Proton.Sdk.Drive.CExports;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct InteropRevisionForTransfer
{
    public readonly InteropArray Id;
    public readonly byte State;
    public readonly InteropArray ManifestSignature;
    public readonly InteropArray SignatureEmailAddress;
    public readonly InteropArray SampleSha256Digests;

    public IRevisionForTransfer ToManaged()
    {
        return new RevisionForTransfer
        {
            Id = new RevisionId(Id.Utf8ToString()),
            State = (RevisionState)State,
            ManifestSignature = ManifestSignature.ToArrayOrNull(),
            SignatureEmailAddress = SignatureEmailAddress.Utf8ToStringOrNull(),
            SamplesSha256Digests = [],
        };
    }

    private sealed class RevisionForTransfer : IRevisionForTransfer
    {
        public required RevisionId Id { get; init; }

        public required RevisionState State { get; init; }

        public required ReadOnlyMemory<byte>? ManifestSignature { get; init; }

        public required string? SignatureEmailAddress { get; init; }

        public required IReadOnlyList<ReadOnlyMemory<byte>> SamplesSha256Digests { get; init; }
    }
}
