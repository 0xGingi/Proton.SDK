namespace Proton.Sdk.Addresses;

internal sealed class AddressResponse : ApiResponse
{
    public required AddressDto Address { get; init; }
}
