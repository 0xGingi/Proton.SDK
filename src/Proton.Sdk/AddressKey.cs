namespace Proton.Sdk;

public sealed class AddressKey(AddressId addressId, AddressKeyId id, bool isAllowedForEncryption)
{
    public AddressId AddressId { get; } = addressId;
    public AddressKeyId Id { get; } = id;
    public bool IsAllowedForEncryption { get; } = isAllowedForEncryption;
}
