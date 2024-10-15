using Proton.Sdk.Cryptography;

namespace Proton.Sdk.Keys;

internal sealed class PublicKeyEntry
{
    public required PublicKeyFlags Flags { get; init; }

    public required PgpArmoredPublicKey PublicKey { get; init; }
}
