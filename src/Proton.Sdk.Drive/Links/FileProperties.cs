using Proton.Sdk.Cryptography;
using Proton.Sdk.Drive.Files;

namespace Proton.Sdk.Drive.Links;

internal readonly struct FileProperties
{
    public required ReadOnlyMemory<byte> ContentKeyPacket { get; init; }

    public PgpArmoredSignature? ContentKeyPacketSignature { get; init; }

    public required RevisionDto? ActiveRevision { get; init; }
}
