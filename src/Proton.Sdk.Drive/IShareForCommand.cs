namespace Proton.Sdk.Drive;

public interface IShareForCommand
{
    ShareId Id { get; }
    AddressId MembershipAddressId { get; }
    string MembershipEmailAddress { get; }
}
