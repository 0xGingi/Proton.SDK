namespace Proton.Sdk.Addresses;

[Flags]
public enum AddressKeyFlags
{
    None = 0,
    IsAllowedForSignatureVerification = 1,
    IsAllowedForEncryption = 2,
}
