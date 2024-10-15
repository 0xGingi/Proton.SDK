namespace Proton.Sdk.Addresses;

internal sealed class AddressListResponse : ApiResponse
{
    public required IReadOnlyList<AddressDto> Addresses { get; init; }
}
