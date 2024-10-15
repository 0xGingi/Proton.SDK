namespace Proton.Sdk.Keys;

internal sealed record PublicKeyListAddress
{
    public required IReadOnlyList<PublicKeyEntry> Keys { get; init; }
}
