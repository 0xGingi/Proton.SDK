namespace Proton.Sdk.Keys;

internal sealed class KeySaltListResponse : ApiResponse
{
    public required IReadOnlyList<KeySalt> KeySalts { get; init; }
}
